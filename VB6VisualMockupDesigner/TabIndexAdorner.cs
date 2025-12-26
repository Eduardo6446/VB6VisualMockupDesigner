using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;


namespace VB6VisualMockupDesigner
{
    public class TabIndexAdorner : Adorner
    {
        private readonly int _index;

        public TabIndexAdorner(UIElement adornedElement, int index) : base(adornedElement)
        {
            _index = index;
            // Asegura que el adorno no capture clics, permitiendo que lleguen al control subyacente
            this.IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Definir estilo de la etiqueta
            var typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var text = new FormattedText(
                _index.ToString(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                10,
                Brushes.White,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Tamaño del cuadro basado en el texto
            double padding = 3;
            Rect bgRect = new Rect(0, 0, text.Width + (padding * 2), text.Height + (padding * 2));

            // Dibujar fondo azul (estilo clásico de diseñadores)
            drawingContext.DrawRectangle(
                new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                new Pen(Brushes.White, 1),
                bgRect);

            // Dibujar número
            drawingContext.DrawText(text, new Point(padding, padding));
        }
    }
}
