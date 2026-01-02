using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace VB6VisualMockupDesigner
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
            if (_selectedControls.Count == 0) return;

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

        public void Paste()
        {
            if (DesignerClipboard.IsEmpty) return;

            SaveUndoSnapshot();
            ClearSelection();

            double baseX = 10 + _pasteOffset;
            double baseY = 10 + _pasteOffset;

            foreach (var item in DesignerClipboard.Items)
            {
                UIElement newControl = RetroControlFactory.Create(item.ControlType);
                if (newControl is FrameworkElement fe)
                {
                    fe.Width = item.Width;
                    fe.Height = item.Height;

                    if (fe is ContentControl cc) cc.Content = item.ContentText;
                    else if (fe is TextBox tb) tb.Text = item.ContentText;
                    else if (fe is TextBlock txt) txt.Text = item.ContentText;

                    double finalX = baseX + item.RelativeLeft;
                    double finalY = baseY + item.RelativeTop;

                    AddControlToCanvas(newControl, finalX, finalY);
                    AddToSelection(newControl);
                }
            }

            _pasteOffset += 10;
            NotifySelectionChanged();
        }
    
}
}