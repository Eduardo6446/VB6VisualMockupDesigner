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
                // Ignoramos los adornos visuales (Bordes azules, handles, líneas de selección)
                if (child is FrameworkElement fe && !(child is System.Windows.Shapes.Rectangle) && !(child is Border))
                {
                    string text = "";
                    if (fe is ContentControl cc) text = cc.Content?.ToString();
                    else if (fe is TextBox tb) text = tb.Text;
                    else if (fe is TextBlock txt) text = txt.Text;

                    state.Controls.Add(new ControlSnapshot
                    {
                        Type = fe.Tag?.ToString() ?? fe.GetType().Name,
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
            // 1. Limpiar estado actual
            ClearSelection(); // Usamos el método de la clase principal
            DesignSurface.Children.Clear();
            NotifySelectionChanged();

            // 2. Reconstruir controles desde el snapshot
            foreach (var item in state.Controls)
            {
                // Limpieza de tipos antiguos o corruptos
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

                    AddControlToCanvas(newControl, item.Left, item.Top);
                }
            }
        }

        // ==========================================
        // LÓGICA DE COPY / PASTE / CUT (Ctrl+C, V, X)
        // ==========================================

        public void CopySelected()
        {
            if (_selectedControls.Count == 0) return;

            // 1. Limpiar portapapeles
            DesignerClipboard.Clear();

            // 2. Calcular punto de referencia (Top-Left del grupo seleccionado)
            // Esto sirve para pegar el grupo manteniendo su forma relativa
            double minX = _selectedControls.Min(c => Canvas.GetLeft(c));
            double minY = _selectedControls.Min(c => Canvas.GetTop(c));

            foreach (FrameworkElement fe in _selectedControls)
            {
                string text = "";
                if (fe is ContentControl cc) text = cc.Content?.ToString();
                else if (fe is TextBox tb) text = tb.Text;
                else if (fe is TextBlock txt) text = txt.Text;

                DesignerClipboard.Items.Add(new ClipboardItem
                {
                    ControlType = fe.Tag?.ToString(),
                    Width = fe.Width,
                    Height = fe.Height,
                    ContentText = text,
                    // Guardamos qué tan lejos está este control del "inicio" del grupo
                    RelativeLeft = Canvas.GetLeft(fe) - minX,
                    RelativeTop = Canvas.GetTop(fe) - minY
                });
            }

            _pasteOffset = 10; // Reiniciar offset para la nueva copia
        }

        public void CutSelected()
        {
            if (_selectedControls.Count > 0)
            {
                CopySelected();
                SaveUndoSnapshot();
                DeleteSelectedControl(); // Este método ya maneja borrado múltiple en la otra clase parcial
            }
        }

        public void Paste()
        {
            if (DesignerClipboard.IsEmpty) return;

            SaveUndoSnapshot();

            // Deseleccionar lo actual para seleccionar lo que vamos a pegar
            ClearSelection();

            // Punto base de pegado (con efecto cascada)
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

                    // Posición: Base + Posición relativa original
                    double finalX = baseX + item.RelativeLeft;
                    double finalY = baseY + item.RelativeTop;

                    AddControlToCanvas(newControl, finalX, finalY);

                    // Añadir a la selección nueva
                    AddToSelection(newControl);
                }
            }

            // Incrementar offset para el próximo paste
            _pasteOffset += 10;

            // Actualizar visuales (bordes azules) y notificar
            NotifySelectionChanged();
        }
    }
}