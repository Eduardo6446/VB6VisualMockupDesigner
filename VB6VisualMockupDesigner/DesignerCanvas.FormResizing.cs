using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VB6VisualMockupDesigner
{
    // PARTIAL CLASS: Manejo de Redimensión del Formulario Principal
    public partial class DesignerCanvas
    {
        private bool _isResizingForm = false;
        private Point _resizeClickStart;
        private double _initialFormWidth;
        private double _initialFormHeight;

        private void ResizeGrip_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var grip = sender as UIElement;
            _isResizingForm = true;
            _resizeClickStart = e.GetPosition(this);
            _initialFormWidth = WindowResizerGrid.Width;
            _initialFormHeight = WindowResizerGrid.Height;
            grip.CaptureMouse();
            e.Handled = true;
        }

        private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizingForm)
            {
                Point currentPos = e.GetPosition(this);
                double deltaX = currentPos.X - _resizeClickStart.X;
                double deltaY = currentPos.Y - _resizeClickStart.Y;

                double newWidth = SnapToGrid(_initialFormWidth + deltaX);
                double newHeight = SnapToGrid(_initialFormHeight + deltaY);

                if (newWidth < 100) newWidth = 100;
                if (newHeight < 100) newHeight = 100;

                // 1. Redimensionar contenedor padre
                if (WindowResizerGrid != null)
                {
                    WindowResizerGrid.Width = newWidth;
                    WindowResizerGrid.Height = newHeight;
                }

                // 2. Sincronizar el borde visual (Fuerza Bruta para arreglar bug visual)
                if (RetroFormContainer != null)
                {
                    RetroFormContainer.Width = newWidth;
                    RetroFormContainer.Height = newHeight;
                }
            }
        }

        private void ResizeGrip_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizingForm)
            {
                _isResizingForm = false;
                (sender as UIElement).ReleaseMouseCapture();
            }
        }

        public void SetFormDimensions(double width, double height)
        {
            if (width < 100) width = 100;
            if (height < 100) height = 100;

            // Ajustar contenedor
            if (WindowResizerGrid != null)
            {
                WindowResizerGrid.Width = width;
                WindowResizerGrid.Height = height;

                // Centrar en lienzo
                if (DesignGrid != null)
                {
                    double l = (DesignGrid.Width - width) / 2;
                    double t = (DesignGrid.Height - height) / 2;
                    WindowResizerGrid.Margin = new Thickness(Math.Max(0, l), Math.Max(0, t), 0, 0);
                }
            }

            // Ajustar Borde Visual
            if (RetroFormContainer != null)
            {
                RetroFormContainer.Width = width;
                RetroFormContainer.Height = height;
            }
        }
    }
}