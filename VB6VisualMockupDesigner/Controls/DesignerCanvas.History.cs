using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup; // Necesario para XamlWriter/Reader
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using System.Text;
using VB6VisualMockupDesigner.Models;

namespace VB6VisualMockupDesigner.Controls
{
    public partial class DesignerCanvas
    {
        // ==========================================
        // 1. SISTEMA DE UNDO / REDO (Versión Jerárquica XAML)
        // ==========================================

        // Pila de Strings XAML (Snapshots completos del Canvas)
        private Stack<string> _undoStack = new Stack<string>();
        private Stack<string> _redoStack = new Stack<string>();
        private bool _hasSavedUndoForDrag = false;
        private double _pasteOffset = 10;

        public void SaveUndoSnapshot()
        {
            // Usamos el helper seguro
            string xaml = CreateSafeXamlSnapshot();

            if (!string.IsNullOrEmpty(xaml))
            {
                _undoStack.Push(xaml);
                _redoStack.Clear(); // Al hacer una nueva acción, se borra el futuro (Redo)
                GenerateNewVersion();
            }
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            // 1. Guardar estado actual en Redo (usando el helper seguro)
            string currentState = CreateSafeXamlSnapshot();
            _redoStack.Push(currentState);

            // 2. Restaurar estado anterior
            string previousState = _undoStack.Pop();
            RestoreStateFromXaml(previousState);
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            // 1. Guardar estado actual en Undo (usando el helper seguro)
            string currentState = CreateSafeXamlSnapshot();
            _undoStack.Push(currentState);

            // 2. Restaurar estado futuro
            string nextState = _redoStack.Pop();
            RestoreStateFromXaml(nextState);
        }


        private void RestoreStateFromXaml(string xamlState)
        {
            // 1. Limpieza inicial
            ClearSelection();
            DesignSurface.Children.Clear();
            _resizeHandles.Clear();
            _selectionAdorners.Clear();

            try
            {
                var context = new ParserContext();
                context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                // Asegúrate que este namespace sea el correcto de tu proyecto
                context.XmlnsDictionary.Add("local", "clr-namespace:VB6VisualMockupDesigner.Controls;assembly=VB6VisualMockupDesigner");

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xamlState)))
                {
                    Canvas loadedCanvas = XamlReader.Load(stream, context) as Canvas;

                    if (loadedCanvas != null)
                    {
                        // 2. Restaurar controles: Usamos una lista temporal para no modificar la colección mientras iteramos
                        var childrenToMove = new List<UIElement>();
                        foreach (UIElement child in loadedCanvas.Children)
                        {
                            childrenToMove.Add(child);
                        }

                        // Vaciamos el contenedor temporal para romper vínculos
                        loadedCanvas.Children.Clear();

                        foreach (var child in childrenToMove)
                        {
                            // DOBLE SEGURIDAD: Desconectar explícitamente
                            ForceDisconnect(child);

                            DesignSurface.Children.Add(child);

                            if (child is FrameworkElement fe)
                            {
                                WireEventsRecursively(fe);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error Restore: " + ex.Message);
            }

            // 3. Restaurar Herramientas del Sistema
            // AQUI ES DONDE TE DABA EL ERROR ANTES.
            // Ahora usamos ForceDisconnect antes de agregar para asegurar que estén libres.

            if (SelectionRect != null)
            {
                ForceDisconnect(SelectionRect);
                if (!DesignSurface.Children.Contains(SelectionRect)) DesignSurface.Children.Add(SelectionRect);
            }

            if (SnapLineOverlay != null)
            {
                ForceDisconnect(SnapLineOverlay); // <--- ESTO ARREGLA TU ERROR
                if (!DesignSurface.Children.Contains(SnapLineOverlay)) DesignSurface.Children.Add(SnapLineOverlay);
            }

            if (QuickEditBox != null)
            {
                ForceDisconnect(QuickEditBox);
                if (!DesignSurface.Children.Contains(QuickEditBox))
                {
                    DesignSurface.Children.Add(QuickEditBox);
                    Panel.SetZIndex(QuickEditBox, int.MaxValue);
                }
            }

            GenerateNewVersion();
        }


        // ==========================================
        // 2. COPY / PASTE / CUT (Jerárquico)
        // ==========================================

        public void CopySelected()
        {
            if (_selectedControls.Count == 0) return;

            // Usamos XamlWriter para copiar, igual que antes, pero esto ya soporta jerarquía
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

        public void ExecuteCopy()
        {
            try
            {
                var controls = GetSelectedControls();
                if (controls.Count == 0) return;

                // 1. Limpieza de menús específica para la selección
                // (Nota: Copy es especial porque solo copia la selección, no todo el canvas,
                //  así que mantenemos la lógica local, pero aseguramos limpieza de menús)
                _tempMenuStorage.Clear();
                foreach (var item in controls) DetachContextMenusRecursively(item);

                StringBuilder sb = new StringBuilder();
                sb.Append("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:local='clr-namespace:VB6VisualMockupDesigner.Controls;assembly=VB6VisualMockupDesigner'>");

                try
                {
                    foreach (var item in controls)
                    {
                        string xaml = XamlWriter.Save(item);
                        sb.Append(xaml);
                    }
                }
                finally
                {
                    // Restaurar siempre
                    ReattachContextMenus();
                }

                sb.Append("</Canvas>");

                var dataObject = new DataObject();
                dataObject.SetData(DataFormats.Xaml, sb.ToString());
                Clipboard.SetDataObject(dataObject, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al copiar: " + ex.Message);
                ReattachContextMenus(); // Seguridad extra
            }
        }

        public void Paste()
        {
            // 1. Obtener objetos del portapapeles
            List<UIElement> newControls = GetElementsFromClipboard();
            if (newControls.Count == 0) return;

            SaveUndoSnapshot();
            ClearSelection();

            // 2. Determinar destino (¿Pegar en el Form o dentro de un Frame seleccionado?)
            Canvas targetCanvas = DesignSurface;
            FrameworkElement containerElement = null;

            if (_selectedControls.Count == 1)
            {
                // Si hay 1 cosa seleccionada, verificamos si es un contenedor
                var selected = _selectedControls.First() as FrameworkElement;
                Canvas inner = GetInnerCanvas(selected);
                if (inner != null)
                {
                    targetCanvas = inner;
                    containerElement = selected;
                }
            }

            foreach (var obj in newControls)
            {
                if (obj is FrameworkElement newCtrl)
                {
                    // 3. Generar nuevo nombre único
                    if (!string.IsNullOrEmpty(newCtrl.Name))
                    {
                        // Lógica simplificada de renombramiento
                        newCtrl.Name = GenerateUniqueName(newCtrl.Name);
                        // Limpiar índice de array por seguridad al pegar
                        VB6Data.SetIndex(newCtrl, null);
                    }

                    // 4. Calcular posición
                    double l = GetSafeLeft(newCtrl) + _pasteOffset;
                    double t = GetSafeTop(newCtrl) + _pasteOffset;

                    // Si pegamos dentro de un Frame, ajustamos para que no se vaya muy lejos
                    if (targetCanvas != DesignSurface)
                    {
                        l = 10;
                        t = 10;
                        _pasteOffset += 10; // Cascada
                    }

                    Canvas.SetLeft(newCtrl, SnapToGrid(l));
                    Canvas.SetTop(newCtrl, SnapToGrid(t));

                    // 5. Agregar al destino
                    targetCanvas.Children.Add(newCtrl);

                    // 6. ¡CRÍTICO! CONECTAR EVENTOS A TODO EL ÁRBOL (HIJOS INCLUIDOS)
                    WireEventsRecursively(newCtrl);

                    AddToSelection(newCtrl);
                }
            }

            // Si pegamos en el root, aumentamos offset global
            if (targetCanvas == DesignSurface) _pasteOffset += 10;

            NotifySelectionChanged();
        }

        public List<UIElement> GetElementsFromClipboard()
        {
            var list = new List<UIElement>();
            try
            {
                if (!Clipboard.ContainsData(DataFormats.Xaml)) return list;
                string xamlString = Clipboard.GetData(DataFormats.Xaml) as string;
                if (string.IsNullOrEmpty(xamlString)) return list;

                var context = new ParserContext();
                context.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                // Ajusta el namespace a tu proyecto
                context.XmlnsDictionary.Add("local", "clr-namespace:VB6VisualMockupDesigner.Controls;assembly=VB6VisualMockupDesigner");

                var bytes = Encoding.UTF8.GetBytes(xamlString);
                using (var stream = new MemoryStream(bytes))
                {
                    var loadedObject = XamlReader.Load(stream, context);

                    if (loadedObject is Canvas rootCanvas)
                    {
                        var children = new List<UIElement>();
                        foreach (UIElement child in rootCanvas.Children) children.Add(child);
                        rootCanvas.Children.Clear();
                        list.AddRange(children);
                    }
                    else if (loadedObject is UIElement singleElement)
                    {
                        list.Add(singleElement);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error pegar: " + ex.Message);
            }
            return list;
        }

        // ==========================================
        // 3. HELPER RECURSIVO DE EVENTOS (LA MAGIA)
        // ==========================================

        private void WireEventsRecursively(FrameworkElement element)
        {
            if (element == null) return;

            // 1. Conectar eventos al elemento actual (Si es un control nuestro)
            // Filtramos para no conectar eventos a partes internas que no debemos tocar
            if (IsDesignerControl(element))
            {
                element.PreviewMouseDown -= Control_PreviewMouseDown; // Evitar duplicados
                element.PreviewMouseDown += Control_PreviewMouseDown;

                element.PreviewMouseMove -= Control_PreviewMouseMove;
                element.PreviewMouseMove += Control_PreviewMouseMove;

                element.PreviewMouseUp -= Control_PreviewMouseUp;
                element.PreviewMouseUp += Control_PreviewMouseUp;

                if (this.Resources.Contains("ControlContextMenu"))
                {
                    element.ContextMenu = (ContextMenu)this.Resources["ControlContextMenu"];
                }
            }

            // 2. Buscar contenedores hijos y recursar
            // Si es un Frame/PictureBox/Panel, tiene un Canvas dentro con hijos
            Canvas innerCanvas = GetInnerCanvas(element);
            if (innerCanvas != null)
            {
                foreach (UIElement child in innerCanvas.Children)
                {
                    if (child is FrameworkElement feChild)
                    {
                        WireEventsRecursively(feChild);
                    }
                }
            }
            // Caso especial SSPanel (Grid -> Canvas)
            else if (element is Border b && b.Child is Grid g)
            {
                foreach (var grandChild in g.Children)
                {
                    if (grandChild is Canvas c)
                    {
                        foreach (UIElement greatChild in c.Children)
                            if (greatChild is FrameworkElement k) WireEventsRecursively(k);
                    }
                }
            }
        }

        private bool IsDesignerControl(FrameworkElement fe)
        {
            // Identificar si es un control del usuario y no una parte interna (como un scrollbar de un listbox)
            // Nuestra fábrica pone Tags o nombres específicos.
            if (fe.Tag != null) return true;
            if (fe is TextBox || fe is Button || fe is Label || fe is CheckBox || fe is RadioButton || fe is GroupBox || fe is Border) return true;
            return false;
        }


        // ==========================================
        // 4. HELPERS DE NOMBRES Y OTROS
        // ==========================================

        private FrameworkElement FindControlByName(string name)
        {
            // Búsqueda recursiva porque ahora hay jerarquía
            return FindControlRecursive(DesignSurface, name);
        }

        private FrameworkElement FindControlRecursive(Panel parent, string name)
        {
            foreach (UIElement child in parent.Children)
            {
                if (child is FrameworkElement fe)
                {
                    if (string.Equals(fe.Name, name, StringComparison.OrdinalIgnoreCase)) return fe;

                    // Si es contenedor, buscar dentro
                    Canvas inner = GetInnerCanvas(fe);
                    if (inner != null)
                    {
                        var found = FindControlRecursive(inner, name);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }

        private string GenerateUniqueName(string baseName)
        {
            int i = 1;
            // Quitamos números al final para no generar Command111
            string root = System.Text.RegularExpressions.Regex.Replace(baseName, @"[\d-]", string.Empty);

            while (true)
            {
                string candidate = $"{root}{i}";
                if (FindControlByName(candidate) == null) return candidate;
                i++;
            }
        }

        // --- HELPERS PARA EVITAR EL ERROR DE DUPLICATE NAME EN XAMLWRITER ---

        private Dictionary<FrameworkElement, ContextMenu> _tempMenuStorage = new Dictionary<FrameworkElement, ContextMenu>();

        // Quita los menús de un elemento y todos sus hijos recursivamente
        private void DetachContextMenusRecursively(FrameworkElement element)
        {
            if (element == null) return;

            // 1. Guardar y quitar menú del elemento actual
            if (element.ContextMenu != null)
            {
                _tempMenuStorage[element] = element.ContextMenu;
                element.ContextMenu = null;
            }

            // 2. Recorrer hijos (usando tu helper GetInnerCanvas para contenedores)
            Canvas inner = GetInnerCanvas(element);
            if (inner != null)
            {
                foreach (UIElement child in inner.Children)
                {
                    if (child is FrameworkElement feChild)
                        DetachContextMenusRecursively(feChild);
                }
            }
            // Caso especial SSPanel que tiene Grid
            else if (element is Border b && b.Child is Grid g)
            {
                foreach (var grandChild in g.Children)
                {
                    if (grandChild is Canvas c)
                    {
                        foreach (UIElement greatChild in c.Children)
                            if (greatChild is FrameworkElement k) DetachContextMenusRecursively(k);
                    }
                }
            }
        }

        // Restaura los menús que quitamos
        private void ReattachContextMenus()
        {
            foreach (var kvp in _tempMenuStorage)
            {
                kvp.Key.ContextMenu = kvp.Value;
            }
            _tempMenuStorage.Clear();
        }

        // Helper para desconectar un elemento de CUALQUIER padre que tenga
        private void ForceDisconnect(UIElement element)
        {
            if (element == null) return;

            // 1. Verificar padre visual (Capa de dibujo)
            var visualParent = VisualTreeHelper.GetParent(element) as Panel;
            if (visualParent != null)
            {
                visualParent.Children.Remove(element);
            }

            // 2. Verificar padre lógico (Capa de objetos)
            // A veces el padre visual es null pero el lógico no.
            if (element is FrameworkElement fe && fe.Parent is Panel logicalParentPanel)
            {
                if (logicalParentPanel.Children.Contains(element))
                {
                    logicalParentPanel.Children.Remove(element);
                }
            }
        }

        // Método maestro para tomar fotos del Canvas sin romper WPF
        private string CreateSafeXamlSnapshot()
        {
            // 1. PREPARACIÓN: Quitar cosas que no queremos guardar o que rompen XamlWriter

            // A. Quitar Menús (Evita error "Duplicate name MnuLockItem")
            _tempMenuStorage.Clear();
            foreach (UIElement child in DesignSurface.Children)
            {
                if (child is FrameworkElement fe) DetachContextMenusRecursively(fe);
            }

            // B. Quitar Adornos Visuales (Bordes azules, Handles, Líneas rojas)
            var visualArtifacts = new List<UIElement>();

            // Recolectar para quitar temporalmente
            foreach (var child in DesignSurface.Children.OfType<UIElement>().ToList())
            {
                // Identificamos artefactos visuales
                if (child == SelectionRect ||
                    child == SnapLineOverlay ||
                    child == QuickEditBox ||
                    _resizeHandles.Contains(child) ||
                    _selectionAdorners.Values.Contains(child))
                {
                    visualArtifacts.Add(child);
                    DesignSurface.Children.Remove(child);
                }
            }

            string xaml = "";

            try
            {
                // 2. TOMAR LA FOTO (SERIALIZAR)
                xaml = XamlWriter.Save(DesignSurface);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error generando snapshot: " + ex.Message);
            }
            finally
            {
                // 3. RESTAURAR TODO (Ponerlo como estaba)

                // Restaurar Menús
                ReattachContextMenus();

                // Restaurar Adornos
                foreach (var artifact in visualArtifacts)
                {
                    if (!DesignSurface.Children.Contains(artifact))
                        DesignSurface.Children.Add(artifact);
                }
            }

            return xaml;
        }
    }
}