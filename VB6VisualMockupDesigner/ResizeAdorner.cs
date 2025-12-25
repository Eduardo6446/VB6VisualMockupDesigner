using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace VB6VisualMockupDesigner
{
    public class ResizeAdorner : Adorner
    {
        // Colección de "agarraderas" (Thumbs) visuales
        private VisualCollection _visuals;

        // Los 8 puntos de redimensionamiento
        private Thumb _topLeft, _top, _topRight;
        private Thumb _left, _right;
        private Thumb _bottomLeft, _bottom, _bottomRight;

        // Estilo visual de las agarraderas (cuadraditos azules oscuros tipo VB6)
        private const double ThumbSize = 7;

        public ResizeAdorner(UIElement adornedElement) : base(adornedElement)
        {
            _visuals = new VisualCollection(this);

            // Crear los 8 manejadores
            _topLeft = BuildThumb(Cursors.SizeNWSE);
            _top = BuildThumb(Cursors.SizeNS);
            _topRight = BuildThumb(Cursors.SizeNESW);
            _left = BuildThumb(Cursors.SizeWE);
            _right = BuildThumb(Cursors.SizeWE);
            _bottomLeft = BuildThumb(Cursors.SizeNESW);
            _bottom = BuildThumb(Cursors.SizeNS);
            _bottomRight = BuildThumb(Cursors.SizeNWSE);

            // Suscribir eventos de arrastre
            _bottomRight.DragDelta += (s, e) => Resize(s, e, 0, 0, e.HorizontalChange, e.VerticalChange);
            _bottom.DragDelta += (s, e) => Resize(s, e, 0, 0, 0, e.VerticalChange);
            _right.DragDelta += (s, e) => Resize(s, e, 0, 0, e.HorizontalChange, 0);

            _top.DragDelta += (s, e) => Resize(s, e, 0, e.VerticalChange, 0, -e.VerticalChange);
            _left.DragDelta += (s, e) => Resize(s, e, e.HorizontalChange, 0, -e.HorizontalChange, 0);

            _topLeft.DragDelta += (s, e) => Resize(s, e, e.HorizontalChange, e.VerticalChange, -e.HorizontalChange, -e.VerticalChange);
            _topRight.DragDelta += (s, e) => Resize(s, e, 0, e.VerticalChange, e.HorizontalChange, -e.VerticalChange);
            _bottomLeft.DragDelta += (s, e) => Resize(s, e, e.HorizontalChange, 0, -e.HorizontalChange, e.VerticalChange);
        }

        private Thumb BuildThumb(Cursor cursor)
        {
            var thumb = new Thumb
            {
                Cursor = cursor,
                Width = ThumbSize,
                Height = ThumbSize,
                Background = new SolidColorBrush(Color.FromRgb(0, 0, 100)), // Azul oscuro clásico
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1)
            };
            _visuals.Add(thumb);
            return thumb;
        }

        private void Resize(object sender, DragDeltaEventArgs e, double left, double top, double width, double height)
        {
            if (AdornedElement is FrameworkElement element)
            {
                // Calcular nuevos valores
                double newWidth = Math.Max(element.Width + width, 10);
                double newHeight = Math.Max(element.Height + height, 10);
                double newLeft = Canvas.GetLeft(element) + left;
                double newTop = Canvas.GetTop(element) + top;

                // Snap to Grid (8px)
                if (Math.Abs(width) > 0) newWidth = Math.Round(newWidth / 8) * 8;
                if (Math.Abs(height) > 0) newHeight = Math.Round(newHeight / 8) * 8;
                if (Math.Abs(left) > 0) newLeft = Math.Round(newLeft / 8) * 8;
                if (Math.Abs(top) > 0) newTop = Math.Round(newTop / 8) * 8;

                // Aplicar cambios
                if (newWidth > 0) element.Width = newWidth;
                if (newHeight > 0) element.Height = newHeight;
                Canvas.SetLeft(element, newLeft);
                Canvas.SetTop(element, newTop);

                // Notificar a la ventana principal para actualizar propiedades (opcional, requiere evento)
            }
        }

        protected override Visual GetVisualChild(int index) => _visuals[index];
        protected override int VisualChildrenCount => _visuals.Count;

        protected override Size ArrangeOverride(Size finalSize)
        {
            double w = finalSize.Width;
            double h = finalSize.Height;
            double off = ThumbSize / 2;

            _topLeft.Arrange(new Rect(-off, -off, ThumbSize, ThumbSize));
            _top.Arrange(new Rect(w / 2 - off, -off, ThumbSize, ThumbSize));
            _topRight.Arrange(new Rect(w - off, -off, ThumbSize, ThumbSize));

            _left.Arrange(new Rect(-off, h / 2 - off, ThumbSize, ThumbSize));
            _right.Arrange(new Rect(w - off, h / 2 - off, ThumbSize, ThumbSize));

            _bottomLeft.Arrange(new Rect(-off, h - off, ThumbSize, ThumbSize));
            _bottom.Arrange(new Rect(w / 2 - off, h - off, ThumbSize, ThumbSize));
            _bottomRight.Arrange(new Rect(w - off, h - off, ThumbSize, ThumbSize));

            return finalSize;
        }
    }
}
