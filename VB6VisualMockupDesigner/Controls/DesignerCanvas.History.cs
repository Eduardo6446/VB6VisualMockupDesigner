using System; // <--- Faltaba para Exception
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VB6VisualMockupDesigner.Models; // Para CanvasState, ControlSnapshot
using VB6VisualMockupDesigner.Helpers; // Para RetroControlFactory
using VB6VisualMockupDesigner.Services;
using System.Windows.Input; // <--- Importante para Clipboard

namespace VB6VisualMockupDesigner.Controls
{
    public partial class DesignerCanvas
    {
        // PILAS DE HISTORIAL
        private Stack<CanvasState> _undoStack = new Stack<CanvasState>();
        private Stack<CanvasState> _redoStack = new Stack<CanvasState>();

        // Offset para que al pegar varias veces no queden uno encima de otro
        private double _pasteOffset = 10;

        // ==========================================
        // LÓGICA DE UNDO / REDO (Ctrl+Z, Ctrl+Y)
        // ==========================================

        public void SaveUndoSnapshot()
        {
            var snapshot = GetCurrentState();
            _undoStack.Push(snapshot);
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            _redoStack.Push(GetCurrentState());
            var previousState = _undoStack.Pop();
            RestoreState(previousState);
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            _undoStack.Push(GetCurrentState());
            var nextState = _redoStack.Pop();
            RestoreState(nextState);
        }

        // Helpers de Estado
        private CanvasState GetCurrentState()
        {
            var state = new CanvasState();
            foreach (UIElement child in DesignSurface.Children)
            {
                if (child is FrameworkElement fe && !(child is System.Windows.Shapes.Rectangle) && !(child is Border))
                {
                    string text = "";
                    if (fe is ContentControl cc) text = cc.Content?.ToString();
                    else if (fe is TextBox tb) text = tb.Text;
                    else if (fe is TextBlock txt) text = txt.Text;

                    state.Controls.Add(new ControlSnapshot
                    {
                        Type = fe.Tag?.ToString() ?? fe.GetType().Name,

                        // USAMOS LOS HELPERS AQUÍ:
                        Left = GetSafeLeft(fe),
                        Top = GetSafeTop(fe),

                        // SANITIZACIÓN TAMBIÉN PARA TAMAÑO:
                        Width = double.IsNaN(fe.Width) ? fe.ActualWidth : fe.Width,
                        Height = double.IsNaN(fe.Height) ? fe.ActualHeight : fe.Height,

                        Text = text,
                        Tag = fe.Tag?.ToString()
                    });
                }
            }
            return state;
        }

        private void RestoreState(CanvasState state)
        {
            // 1. Limpiar estado actual
            ClearSelection();
            DesignSurface.Children.Clear();

            // Re-agregar SelectionRect si es necesario (manejado en ClearCanvas, pero por seguridad notificamos)
            ControlSelected?.Invoke(this, null);

            // 2. Reconstruir controles
            foreach (var item in state.Controls)
            {
                string cleanType = item.Type;
                if (string.IsNullOrEmpty(cleanType)) continue;

                UIElement newControl = RetroControlFactory.Create(cleanType);

                if (newControl is FrameworkElement fe)
                {
                    fe.Width = item.Width;
                    fe.Height = item.Height;

                    if (fe is ContentControl cc) cc.Content = item.Text;
                    else if (fe is TextBox tb) tb.Text = item.Text;
                    else if (fe is TextBlock txt) txt.Text = item.Text;

                    fe.Tag = item.Tag;

                    // Usamos el método base para conectar eventos
                    // NOTA: Pasamos las coordenadas, pero luego las forzamos abajo para evitar Doble-Snap
                    AddControlToCanvas(newControl, item.Left, item.Top);

                    // --- CORRECCIÓN CRÍTICA ---
                    // AddControlToCanvas hace SnapToGrid. 
                    // Al restaurar, queremos la posición EXACTA del historial, no redondearla de nuevo.
                    Canvas.SetLeft(newControl, item.Left);
                    Canvas.SetTop(newControl, item.Top);
                }
            }

            // Asegurar que el recuadro de selección esté presente (si se borró en el Clear)
            // (Tu método ClearCanvas ya debería manejar esto, pero no hace daño verificar)
            if (SelectionRect != null && !DesignSurface.Children.Contains(SelectionRect))
            {
                DesignSurface.Children.Add(SelectionRect);
            }
        }

        // ==========================================
        // LÓGICA DE COPY / PASTE / CUT (Ctrl+C, V, X)
        // ==========================================

        public void CopySelected()
        {

            ExecuteCopy();


            if (_selectedControls.Count == 0) return;

            ExecuteCopy();

            DesignerClipboard.Clear();

            // === CORRECCIÓN 1: Sanitizar el cálculo del punto mínimo (Ancla del grupo) ===
            // Si Canvas.GetLeft devuelve NaN, asumimos que es 0.
            double minX = _selectedControls.Min(c =>
            {

                double v = Canvas.GetLeft(c);
                return double.IsNaN(v) ? 0 : v;
            });

            double minY = _selectedControls.Min(c =>
            {
                double v = Canvas.GetTop(c);
                return double.IsNaN(v) ? 0 : v;
            });

            foreach (FrameworkElement fe in _selectedControls)
            {
                string text = "";
                if (fe is ContentControl cc) text = cc.Content?.ToString();
                else if (fe is TextBox tb) text = tb.Text;
                else if (fe is TextBlock txt) text = txt.Text;

                // === CORRECCIÓN 2: Obtener coordenadas seguras para el item actual ===
                double currentL = Canvas.GetLeft(fe);
                double currentT = Canvas.GetTop(fe);

                if (double.IsNaN(currentL)) currentL = 0;
                if (double.IsNaN(currentT)) currentT = 0;

                DesignerClipboard.Items.Add(new ClipboardItem
                {
                    ControlType = fe.Tag?.ToString(),
                    Width = double.IsNaN(fe.Width) ? fe.ActualWidth : fe.Width,
                    Height = double.IsNaN(fe.Height) ? fe.ActualHeight : fe.Height,
                    ContentText = text,

                    // Ahora la matemática es segura: Numero - Numero = Numero
                    RelativeLeft = currentL - minX,
                    RelativeTop = currentT - minY
                });
            }
            ExecuteCopy();


            _pasteOffset = 10;
        }

        public void CutSelected()
        {
            if (_selectedControls.Count > 0)
            {
                CopySelected();
                SaveUndoSnapshot();
                DeleteSelectedControl();
            }
        }

        // Este método ahora solo devuelve la lista de objetos recuperados, NO los agrega al canvas todavía
        public List<UIElement> GetElementsFromClipboard()
        {
            var list = new List<UIElement>();
            try
            {
                // 1. Verificar si hay datos
                if (!Clipboard.ContainsData(DataFormats.Xaml)) return list;

                string xamlString = Clipboard.GetData(DataFormats.Xaml) as string;
                if (string.IsNullOrEmpty(xamlString)) return list;

                // 2. Configurar el Contexto (Esencial para encontrar VB6Data)
                var context = new System.Windows.Markup.ParserContext();
                context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                // Asegúrate de que este namespace coincida con tu proyecto
                context.XmlnsDictionary.Add("local", "clr-namespace:VB6VisualMockupDesigner;assembly=VB6VisualMockupDesigner");

                // 3. SOLUCIÓN A TUS ERRORES: Usar MemoryStream
                // Convertimos el string a bytes y usamos un Stream, que es lo que XamlReader prefiere.
                var bytes = System.Text.Encoding.UTF8.GetBytes(xamlString);

                using (var stream = new System.IO.MemoryStream(bytes))
                {
                    // Ahora sí usamos Load(Stream, ParserContext) que es la sobrecarga más segura
                    var loadedObject = System.Windows.Markup.XamlReader.Load(stream, context);

                    if (loadedObject is Canvas rootCanvas)
                    {
                        // Caso A: Copiamos varios controles (vienen en el Canvas contenedor)
                        var children = new List<UIElement>();
                        foreach (UIElement child in rootCanvas.Children) children.Add(child);

                        rootCanvas.Children.Clear();
                        list.AddRange(children);
                    }
                    else if (loadedObject is UIElement singleElement)
                    {
                        // Caso B: Copiamos un solo control sin contenedor
                        list.Add(singleElement);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al leer portapapeles: " + ex.Message);
                MessageBox.Show("Error al pegar: " + ex.Message);
            }
            return list;
        }

        public void Paste()
        {
            // 1. Recuperar objetos limpios del portapapeles
            List<UIElement> newControls = GetElementsFromClipboard();
            if (newControls.Count == 0) return;

            SaveUndoSnapshot();
            ClearSelection();

            double offset = 20; // Desplazamiento visual para que no queden encima

            foreach (var obj in newControls)
            {
                if (obj is FrameworkElement newCtrl)
                {
                    // 2. Calcular nueva posición
                    double l = GetSafeLeft(newCtrl) + offset;
                    double t = GetSafeTop(newCtrl) + offset;
                    Canvas.SetLeft(newCtrl, l);
                    Canvas.SetTop(newCtrl, t);

                    // 3. LÓGICA DE NOMBRES / ARRAYS
                    string intendedName = newCtrl.Name;
                    FrameworkElement existingCtrl = FindControlByName(intendedName);

                    if (existingCtrl != null && !string.IsNullOrEmpty(intendedName))
                    {
                        var result = MessageBox.Show(
                            $"Ya existe un control llamado '{intendedName}'.\n¿Desea crear una matriz de controles (Control Array)?",
                            "Conflicto de Nombres",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Cancel) continue; // Saltamos este control

                        if (result == MessageBoxResult.Yes) // CREAR ARRAY
                        {
                            // Asignar índice al original si no tiene
                            int? existingIndex = VB6Data.GetIndex(existingCtrl);
                            if (existingIndex == null) VB6Data.SetIndex(existingCtrl, 0);

                            // Buscar siguiente índice libre
                            int nextIndex = GetNextAvailableIndex(intendedName);

                            newCtrl.Name = intendedName; // Mismo nombre
                            VB6Data.SetIndex(newCtrl, nextIndex); // Nuevo índice
                        }
                        else // NO (RENOMBRAR)
                        {
                            newCtrl.Name = GenerateUniqueName(intendedName);
                            VB6Data.SetIndex(newCtrl, null); // Sin índice
                        }
                    }
                    else
                    {
                        // Nombre libre, pero limpiamos el Index por si venía copiado de un array
                        // A menos que quieras copiar el índice también, pero usualmente al copiar y pegar libre
                        // se espera un control nuevo independiente.
                        VB6Data.SetIndex(newCtrl, null);
                    }

                    // 4. Agregar al Canvas y Seleccionar
                    DesignSurface.Children.Add(newCtrl);

                    // Reconectar eventos
                    newCtrl.PreviewMouseDown += Control_PreviewMouseDown;
                    newCtrl.PreviewMouseMove += Control_PreviewMouseMove;
                    newCtrl.PreviewMouseUp += Control_PreviewMouseUp;

                    AddToSelection(newCtrl);
                }
            }

            NotifySelectionChanged();
        }

        // --- MÉTODOS AUXILIARES PARA EL PEGADO ---

        private FrameworkElement FindControlByName(string name)
        {
            foreach (UIElement child in DesignSurface.Children)
            {
                if (child is FrameworkElement fe && string.Equals(fe.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return fe;
                }
            }
            return null;
        }

        private int GetNextAvailableIndex(string name)
        {
            int maxIndex = -1;
            foreach (UIElement child in DesignSurface.Children)
            {
                if (child is FrameworkElement fe && string.Equals(fe.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    int? idx = VB6Data.GetIndex(fe);
                    if (idx.HasValue && idx.Value > maxIndex)
                    {
                        maxIndex = idx.Value;
                    }
                }
            }
            return maxIndex + 1;
        }

        private string GenerateUniqueName(string baseName)
        {
            // Si el nombre termina en número (ej: Command1), intentamos seguir la secuencia (Command2)
            // Si no (ej: cmdAceptar), agregamos 1 (cmdAceptar1)

            // Lógica simple: Probar baseName + "1", "2", "3"...
            int i = 1;
            while (true)
            {
                // Intentamos detectar si baseName ya tiene número al final para incrementarlo inteligentemente
                // Por simplicidad ahora, concatenamos.
                string candidate = $"{baseName}{i}";
                if (FindControlByName(candidate) == null)
                {
                    return candidate;
                }
                i++;
            }
        }


        public void ExecuteCopy()
        {
            try
            {
                var controls = GetSelectedControls();
                if (controls.Count == 0) return;

                // 1. Envolver en un root temporal para que sea XML válido
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:local='clr-namespace:VB6VisualMockupDesigner;assembly=VB6VisualMockupDesigner'>");

                foreach (var item in controls)
                {
                    try
                    {
                        // Guardamos cada control
                        string xaml = System.Windows.Markup.XamlWriter.Save(item);
                        sb.Append(xaml);
                    }
                    catch (Exception exSerial) { System.Diagnostics.Debug.WriteLine("Error serializando: " + exSerial.Message); }
                }
                sb.Append("</Canvas>");

                // 2. Enviar al portapapeles
                var dataObject = new DataObject();
                dataObject.SetData(DataFormats.Xaml, sb.ToString());
                Clipboard.SetDataObject(dataObject, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al copiar: " + ex.Message);
            }
        }


    }
}