using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VB6VisualMockupDesigner.Models; // Requiere .NET Core 3.1 o superior (o NuGet en .NET Framework)
using VB6VisualMockupDesigner.Helpers;
using VB6VisualMockupDesigner.Services;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Views;

namespace VB6VisualMockupDesigner.Controls
{
    // PARTIAL CLASS: Manejo de Redimensión del Formulario Principal
    public partial class DesignerCanvas
    {
        // Variables específicas para el Formulario (Renombradas para no chocar con los Controles)
        private bool _isResizingForm = false;
        private Point _formResizeClickStart;
        private double _initialFormWidth;
        private double _initialFormHeight;

        private void ResizeGrip_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var grip = sender as UIElement;
            _isResizingForm = true;

            // Usamos 'this' para obtener la posición relativa al Canvas general
            _formResizeClickStart = e.GetPosition(this);

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

                // Calcular diferencia
                double deltaX = currentPos.X - _formResizeClickStart.X;
                double deltaY = currentPos.Y - _formResizeClickStart.Y;

                // Calcular nuevo tamaño aplicando SnapToGrid (Método definido en la otra parte de la clase)
                double newWidth = SnapToGrid(_initialFormWidth + deltaX);
                double newHeight = SnapToGrid(_initialFormHeight + deltaY);

                // Restricciones mínimas (para no desaparecer el form)
                if (newWidth < 100) newWidth = 100;
                if (newHeight < 100) newHeight = 100;

                // 1. Redimensionar contenedor padre (Grid principal del mock)
                if (WindowResizerGrid != null)
                {
                    WindowResizerGrid.Width = newWidth;
                    WindowResizerGrid.Height = newHeight;
                }

                // 2. Sincronizar el borde visual (El estilo "Retro")
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

        // Método público para establecer dimensiones desde fuera (ej: al cargar un archivo .frm)
        public void SetFormDimensions(double width, double height)
        {
            if (width < 100) width = 100;
            if (height < 100) height = 100;

            // Ajustar contenedor
            if (WindowResizerGrid != null)
            {
                WindowResizerGrid.Width = width;
                WindowResizerGrid.Height = height;

                // Centrar el formulario en el lienzo grande
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