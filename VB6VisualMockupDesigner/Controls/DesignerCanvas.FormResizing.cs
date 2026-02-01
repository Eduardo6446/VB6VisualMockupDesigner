using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VB6VisualMockupDesigner.Controls
{
    public partial class DesignerCanvas
    {
        // Estados del Formulario
        private bool _isMovingForm = false;
        private bool _isResizingForm = false;

        // Puntos de inicio
        private Point _formClickStartPoint;
        private Thickness _initialFormMargin;
        private double _initialFormWidth;
        private double _initialFormHeight;

        // Dirección de redimensión (usaremos Tags en el XAML: "TopLeft", "BottomRight", etc.)
        private string _currentResizeDirection = "";

        // ==========================================
        // 1. MOVER EL FORMULARIO (Desde la Barra de Título)
        // ==========================================
        private void FormTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsRunMode) return;

            // Solo si hacemos clic en el borde/titulo, no en los botones de cerrar/min
            if (e.OriginalSource is Button) return;

            var element = sender as UIElement;
            _isMovingForm = true;
            _formClickStartPoint = e.GetPosition(this); // Posición en el Canvas contenedor
            _initialFormMargin = WindowResizerGrid.Margin;

            element.CaptureMouse();
            e.Handled = true;
        }

        private void FormTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsRunMode) return;
            if (_isMovingForm)
            {
                Point currentPos = e.GetPosition(this);
                double deltaX = currentPos.X - _formClickStartPoint.X;
                double deltaY = currentPos.Y - _formClickStartPoint.Y;

                // Movemos aplicando Margen
                double newLeft = Math.Max(0, _initialFormMargin.Left + deltaX);
                double newTop = Math.Max(0, _initialFormMargin.Top + deltaY);

                WindowResizerGrid.Margin = new Thickness(newLeft, newTop, 0, 0);
            }
        }

        private void FormTitle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsRunMode) return;
            if (_isMovingForm)
            {
                _isMovingForm = false;
                (sender as UIElement).ReleaseMouseCapture();
            }
        }

        // ==========================================
        // 2. REDIMENSIONAR EL FORMULARIO (Desde 8 puntos)
        // ==========================================
        private void ResizeForm_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsRunMode) return;

            var rect = sender as System.Windows.Shapes.Rectangle;
            if (rect == null) return;

            _isResizingForm = true;
            _currentResizeDirection = rect.Tag.ToString();

            _formClickStartPoint = e.GetPosition(this);
            _initialFormWidth = WindowResizerGrid.ActualWidth;
            _initialFormHeight = WindowResizerGrid.ActualHeight;
            _initialFormMargin = WindowResizerGrid.Margin;

            rect.CaptureMouse();
            e.Handled = true;
        }

        private void ResizeForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsRunMode) return;

            if (_isResizingForm)
            {
                Point currentPos = e.GetPosition(this);

                // Snap básico (opcional, aquí lo hacemos fluido, si quieres snap usa SnapToGrid)
                double deltaX = currentPos.X - _formClickStartPoint.X;
                double deltaY = currentPos.Y - _formClickStartPoint.Y;

                double newW = _initialFormWidth;
                double newH = _initialFormHeight;
                double newLeft = _initialFormMargin.Left;
                double newTop = _initialFormMargin.Top;

                // Lógica según dirección
                if (_currentResizeDirection.Contains("Right"))
                {
                    newW = Math.Max(100, _initialFormWidth + deltaX);
                }
                if (_currentResizeDirection.Contains("Bottom"))
                {
                    newH = Math.Max(100, _initialFormHeight + deltaY);
                }
                if (_currentResizeDirection.Contains("Left"))
                {
                    // Al crecer a la izquierda: Aumenta Ancho, Disminuye Margen Left
                    double proposedWidth = _initialFormWidth - deltaX;
                    if (proposedWidth >= 100)
                    {
                        newW = proposedWidth;
                        newLeft = _initialFormMargin.Left + deltaX;
                    }
                }
                if (_currentResizeDirection.Contains("Top"))
                {
                    // Al crecer arriba: Aumenta Alto, Disminuye Margen Top
                    double proposedHeight = _initialFormHeight - deltaY;
                    if (proposedHeight >= 100)
                    {
                        newH = proposedHeight;
                        newTop = _initialFormMargin.Top + deltaY;
                    }
                }

                // Aplicar cambios
                WindowResizerGrid.Width = newW;
                WindowResizerGrid.Height = newH;
                WindowResizerGrid.Margin = new Thickness(newLeft, newTop, 0, 0);

                // Sincronizar borde visual interno
                if (RetroFormContainer != null)
                {
                    RetroFormContainer.Width = newW;
                    RetroFormContainer.Height = newH;
                }
            }
        }

        private void ResizeForm_MouseUp(object sender, MouseButtonEventArgs e)
        {

            if (_isResizingForm)
            {
                _isResizingForm = false;
                (sender as UIElement).ReleaseMouseCapture();
            }
        }

        public void SetFormDimensions(double width, double height)
        {
            if (WindowResizerGrid != null)
            {
                WindowResizerGrid.Width = width;
                WindowResizerGrid.Height = height;
                CenterFormOnCanvas(); // Re-centrar al establecer tamaño manual
            }
            if (RetroFormContainer != null)
            {
                RetroFormContainer.Width = width;
                RetroFormContainer.Height = height;
            }
        }
    }
}