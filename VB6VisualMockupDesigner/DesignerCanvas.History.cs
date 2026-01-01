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
            // 1. Crear una foto del estado actual
            var snapshot = GetCurrentState();

            // 2. Guardar en la pila de deshacer
            _undoStack.Push(snapshot);

            // 3. Limpiar la pila de rehacer (si haces algo nuevo, rompes la línea temporal futura)
            _redoStack.Clear();
        }

        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            // 1. Guardar estado actual en Redo antes de volver atrás
            _redoStack.Push(GetCurrentState());

            // 2. Sacar el último estado guardado
            var previousState = _undoStack.Pop();

            // 3. Restaurar
            RestoreState(previousState);
        }

        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            // 1. Guardar estado actual en Undo
            _undoStack.Push(GetCurrentState());

            // 2. Sacar el estado futuro
            var nextState = _redoStack.Pop();

            // 3. Restaurar
            RestoreState(nextState);
        }

        // Helpers de Estado
        private CanvasState GetCurrentState()
        {
            var state = new CanvasState();
            foreach (UIElement child in DesignSurface.Children)
            {
                if (child is FrameworkElement fe && !(child is Border)) // Ignorar selección visual
                {
                    // Guardar propiedades
                    string text = "";
                    if (fe is ContentControl cc) text = cc.Content?.ToString();
                    else if (fe is TextBox tb) text = tb.Text;
                    else if (fe is TextBlock txt) text = txt.Text;

                    state.Controls.Add(new ControlSnapshot
                    {
                        Type = fe.Tag?.ToString() ?? fe.GetType().Name, // Usamos el Tag puesto por la Factory
                        Left = Canvas.GetLeft(fe),
                        Top = Canvas.GetTop(fe),
                        Width = fe.Width,
                        Height = fe.Height,
                        Text = text,
                        Tag = fe.Tag?.ToString()
                    });
                }
            }
            return state;
        }

        private void RestoreState(CanvasState state)
        {
            // 1. Limpiar todo (menos selección si quieres, pero mejor limpiar todo)
            ShowSelectionIndicator(null);
            _selectedControl = null;
            DesignSurface.Children.Clear();

            // Avisar que no hay selección
            ControlSelected?.Invoke(this, null);

            // 2. Reconstruir controles
            foreach (var item in state.Controls)
            {
                // Limpiar el tipo si viene sucio (ej: "Array: 1")
                string cleanType = item.Type;
                if (cleanType.StartsWith("Array")) cleanType = "PictureBox"; // Fallback simple o lógica compleja

                UIElement newControl = RetroControlFactory.Create(cleanType);
                if (newControl is FrameworkElement fe)
                {
                    fe.Width = item.Width;
                    fe.Height = item.Height;

                    if (fe is ContentControl cc) cc.Content = item.Text;
                    else if (fe is TextBox tb) tb.Text = item.Text;
                    else if (fe is TextBlock txt) txt.Text = item.Text;

                    fe.Tag = item.Tag; // Restaurar Tag original

                    AddControlToCanvas(newControl, item.Left, item.Top);
                }
            }
        }

        // ==========================================
        // LÓGICA DE COPY / PASTE / CUT (Ctrl+C, V, X)
        // ==========================================

        public void CopySelected()
        {
            if (_selectedControl is FrameworkElement fe)
            {
                DesignerClipboard.ControlType = fe.Tag?.ToString(); // Importante: Factory pone el Tag
                DesignerClipboard.Width = fe.Width;
                DesignerClipboard.Height = fe.Height;

                if (fe is ContentControl cc) DesignerClipboard.ContentText = cc.Content?.ToString();
                else if (fe is TextBox tb) DesignerClipboard.ContentText = tb.Text;
                else if (fe is TextBlock txt) DesignerClipboard.ContentText = txt.Text;

                _pasteOffset = 10; // Resetear offset
            }
        }

        public void CutSelected()
        {
            if (_selectedControl != null)
            {
                CopySelected();
                SaveUndoSnapshot(); // Guardar historia antes de borrar
                DeleteSelectedControl();
            }
        }

        public void Paste()
        {
            if (DesignerClipboard.IsEmpty) return;

            SaveUndoSnapshot(); // Guardar historia antes de agregar

            UIElement newControl = RetroControlFactory.Create(DesignerClipboard.ControlType);
            if (newControl is FrameworkElement fe)
            {
                fe.Width = DesignerClipboard.Width;
                fe.Height = DesignerClipboard.Height;

                if (fe is ContentControl cc) cc.Content = DesignerClipboard.ContentText;
                else if (fe is TextBox tb) tb.Text = DesignerClipboard.ContentText;
                else if (fe is TextBlock txt) txt.Text = DesignerClipboard.ContentText;

                // Calcular posición: Intentar pegar en el centro o con offset
                double x = 10 + _pasteOffset;
                double y = 10 + _pasteOffset;
                _pasteOffset += 10; // Incrementar para el siguiente paste

                AddControlToCanvas(newControl, x, y);

                // Seleccionar lo nuevo
                ShowSelectionIndicator(newControl);
                _selectedControl = newControl;
                ControlSelected?.Invoke(this, newControl as FrameworkElement);
            }
        }
    }
}