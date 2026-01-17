using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VB6VisualMockupDesigner.Views
{
    public partial class ColorPicker : UserControl
    {
        // Evento para avisar al padre que se eligió un color
        public event Action<string> ColorSelected;

        public ColorPicker()
        {
            InitializeComponent();
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Background is SolidColorBrush brush)
            {
                // Devolvemos el color en formato Hexadecimal (#AARRGGBB)
                string hex = brush.Color.ToString();
                ColorSelected?.Invoke(hex);
            }
        }
    }
}