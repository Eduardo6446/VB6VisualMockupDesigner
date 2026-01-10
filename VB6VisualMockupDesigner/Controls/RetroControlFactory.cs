using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VB6VisualMockupDesigner.Controls
{
    public static class RetroControlFactory
    {
        public static UIElement Create(string type)
        {
            FrameworkElement element = null;

            // Estilos y recursos comunes
            var fontParams = new { Family = new FontFamily("Microsoft Sans Serif"), Size = 11.0 };
            var vbGray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8"));
            var vbBlack = Brushes.Black;

            switch (type)
            {
                case "Pointer": return null;

                case "PictureBox":
                    var picBorder = new Border
                    {
                        Width = 100,
                        Height = 100,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(2),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect { ShadowDepth = 0, BlurRadius = 0 },
                        ClipToBounds = true // IMPORTANTE: Recorta contenido
                    };
                    picBorder.Child = new Canvas(); // Contenedor interno
                    element = picBorder;
                    break;

                case "Label":
                    element = new Label { Content = "Label1", Width = 121, Height = 25, FontFamily = fontParams.Family, FontSize = fontParams.Size, Padding = new Thickness(2) };
                    break;

                case "TextBox":
                    element = new TextBox { Text = "Text1", Width = 121, Height = 25, FontFamily = fontParams.Family, FontSize = fontParams.Size, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
                    break;

                case "Frame":
                    var grp = new GroupBox
                    {
                        Header = "Frame1",
                        Width = 200, // Un poco más grande para probar bien
                        Height = 150,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray
                    };

                    // === SOLUCIÓN CRÍTICA ===
                    var innerCanvas = new Canvas();

                    // 1. OBLIGAR a llenar el espacio
                    innerCanvas.HorizontalAlignment = HorizontalAlignment.Stretch;
                    innerCanvas.VerticalAlignment = VerticalAlignment.Stretch;

                    // 2. OBLIGAR a ser detectable (El 'null' no detecta clicks, 'Transparent' sí)
                    innerCanvas.Background = Brushes.Transparent;

                    // 3. OBLIGAR a tener tamaño mínimo (WPF a veces colapsa Canvas vacíos)
                    innerCanvas.MinHeight = 100;
                    innerCanvas.MinWidth = 100;

                    grp.Content = innerCanvas;
                    // ========================

                    element = grp;
                    break;

                case "CommandButton":
                    element = new Button { Content = "Command1", Width = 121, Height = 33, Background = vbGray, FontFamily = fontParams.Family, FontSize = fontParams.Size };
                    break;

                case "CheckBox":
                    element = new CheckBox { Content = "Check1", Width = 121, Height = 25, FontFamily = fontParams.Family, FontSize = fontParams.Size, VerticalContentAlignment = VerticalAlignment.Center };
                    break;

                case "OptionButton":
                    element = new RadioButton { Content = "Option1", Width = 121, Height = 25, FontFamily = fontParams.Family, FontSize = fontParams.Size, VerticalContentAlignment = VerticalAlignment.Center };
                    break;

                case "ComboBox":
                    element = new ComboBox { Width = 121, Height = 21, FontFamily = fontParams.Family, FontSize = fontParams.Size, IsEditable = true, Text = "Combo1" };
                    break;

                case "ListBox":
                    var list = new ListBox { Width = 121, Height = 100, FontFamily = fontParams.Family, FontSize = fontParams.Size, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
                    list.Items.Add("List1");
                    element = list;
                    break;

                case "HScrollBar":
                    element = new ScrollBar { Orientation = Orientation.Horizontal, Width = 100, Height = 17, Value = 50, Maximum = 100 };
                    break;

                case "VScrollBar":
                    element = new ScrollBar { Orientation = Orientation.Vertical, Width = 17, Height = 100, Value = 50, Maximum = 100 };
                    break;

                case "Timer":
                    element = new Border { Width = 34, Height = 34, Background = vbGray, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1), Child = new TextBlock { Text = "Timer", FontSize = 8, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center } };
                    break;

                case "DriveListBox":
                    element = new ComboBox { Width = 121, Height = 21, FontFamily = fontParams.Family, FontSize = fontParams.Size, Text = @"c: [OS]" };
                    break;

                case "DirListBox":
                    var dirList = new ListBox { Width = 121, Height = 100, FontFamily = fontParams.Family, FontSize = fontParams.Size, BorderBrush = Brushes.Gray };
                    dirList.Items.Add(@"c:\"); dirList.Items.Add(@"  Windows");
                    element = dirList;
                    break;

                case "FileListBox":
                    var fileList = new ListBox { Width = 121, Height = 100, FontFamily = fontParams.Family, FontSize = fontParams.Size, BorderBrush = Brushes.Gray };
                    fileList.Items.Add("archivo1.txt");
                    element = fileList;
                    break;

                case "Shape":
                    element = new System.Windows.Shapes.Rectangle { Width = 50, Height = 50, Stroke = vbBlack, StrokeThickness = 1, Fill = Brushes.Transparent };
                    break;

                case "Line":
                    element = new System.Windows.Shapes.Rectangle { Width = 100, Height = 2, Fill = vbBlack };
                    break;

                case "Image":
                    var imgGrid = new Grid { Width = 100, Height = 100, Background = Brushes.Transparent };
                    imgGrid.Children.Add(new System.Windows.Shapes.Rectangle { Stroke = Brushes.Gray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection() { 4, 2 }, Fill = Brushes.Transparent, IsHitTestVisible = false });
                    imgGrid.Children.Add(new TextBlock { Text = "Image", Foreground = Brushes.Gray, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false });
                    element = imgGrid;
                    break;

                case "Data":
                    // (Omitido código largo del Data para brevedad, copiar del anterior si se desea)
                    element = new Button { Content = "Data Control", Width = 150, Height = 25, Background = vbGray };
                    break;

                case "OLE":
                    element = new Border { Width = 75, Height = 75, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1), Background = Brushes.LightGray, Child = new TextBlock { Text = "OLE", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
                    break;

                default:
                    // PLACEHOLDER ELEGANTE
                    var placeholder = new Grid
                    {
                        Width = 50,
                        Height = 50,
                        Background = new SolidColorBrush(Color.FromRgb(240, 240, 240))
                    };
                    placeholder.Children.Add(new Rectangle
                    {
                        Stroke = Brushes.Gray,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection() { 4, 2 },
                        Fill = Brushes.Transparent
                    });
                    placeholder.Children.Add(new TextBlock
                    {
                        Text = $"[{type}]",
                        Foreground = Brushes.DarkGray,
                        FontSize = 9,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    });
                    element = placeholder;
                    break;
            }

            if (element != null) element.Tag = type;
            return element;
        }
    }
}
