using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VB6VisualMockupDesigner.Helpers
{
    public static class VbHelpers
    {
        public const int PixelsToTwips = 15;

        public static Brush GetVbGray() => new SolidColorBrush(Color.FromRgb(212, 208, 200));

        public static Grid CreateTimerVisual()
        {
            Grid g = new Grid { Width = 32, Height = 32 };
            g.Children.Add(new Ellipse { Stroke = Brushes.Black, StrokeThickness = 1 });
            // Usamos Geometry.Parse para simplicidad en el helper
            g.Children.Add(new Path { Data = Geometry.Parse("M16,4 L16,16 L22,22"), Stroke = Brushes.Black, StrokeThickness = 1 });
            return g;
        }

        public static string GetControlText(FrameworkElement fe)
        {
            if (fe is TextBox txt) return txt.Text;
            if (fe is GroupBox gb) return gb.Header?.ToString();
            if (fe is ContentControl cc) return cc.Content?.ToString(); // Buttons, Labels, CheckBox
            if (fe is ComboBox cmb) return cmb.Text;

            // LOGICA PARA MENU: Devuelve "Item1,Item2,Item3"
            if (fe is Menu mnu)
            {
                var items = new List<string>();
                foreach (var item in mnu.Items) if (item is MenuItem mi) items.Add(mi.Header.ToString());
                return string.Join(",", items);
            }

            // LOGICA PARA STATUSBAR: Devuelve el texto del primer panel
            if (fe is StatusBar sbar)
            {
                if (sbar.Items.Count > 0 && sbar.Items[0] is StatusBarItem sbi) return sbi.Content.ToString();
                return "Status";
            }
            return "";
        }

        public static void SetControlText(FrameworkElement fe, string text)
        {
            if (fe is TextBox txt) txt.Text = text;
            else if (fe is GroupBox gb) gb.Header = text;
            else if (fe is ContentControl cc && !(fe is StatusBarItem)) cc.Content = text;
            else if (fe is ComboBox cmb) cmb.Text = text;

            // LOGICA PARA MENU: Reconstruye los items desde texto separado por comas
            else if (fe is Menu mnu)
            {
                mnu.Items.Clear();
                string[] items = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in items) mnu.Items.Add(new MenuItem { Header = item.Trim() });
            }

            // LOGICA PARA STATUSBAR
            else if (fe is StatusBar sbar)
            {
                sbar.Items.Clear();
                sbar.Items.Add(new StatusBarItem { Content = text });
            }
        }

        public static string MapWpfToVbType(FrameworkElement fe)
        {
            // Soporte para los nuevos tipos
            if (fe is Menu) return "Menu";
            if (fe is StatusBar) return "StatusBar";

            if (fe is Button) return "CommandButton";
            if (fe is TextBox) return "TextBox";
            if (fe is Label) return "Label";
            if (fe is CheckBox) return "CheckBox";
            if (fe is RadioButton) return "OptionButton";
            if (fe is GroupBox) return "Frame";
            if (fe is ComboBox) return "ComboBox";
            if (fe is ListBox) return "ListBox";
            if (fe is Rectangle) return "Shape";
            if (fe is Line) return "Line";
            if (fe is Image) return "Image";
            if (fe is ScrollBar sb) return sb.Orientation == Orientation.Horizontal ? "HScrollBar" : "VScrollBar";
            if (fe is Border) return "PictureBox";
            if (fe is Grid g && g.Children.Count > 0 && g.Children[0] is Ellipse) return "Timer";
            return "";
        }
    }
}