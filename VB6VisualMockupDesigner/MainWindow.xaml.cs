using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;




namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private UIElement _selectedElement = null;
        private Point _clickPosition;
        private const int PixelsToTwips = 15;

        // Variables para el Adorner (Redimensionamiento)
        private AdornerLayer _adornerLayer;
        private ResizeAdorner _currentAdorner;

        // Contadores
        private int _btnCount = 1; private int _lblCount = 1;
        private int _txtCount = 1; private int _chkCount = 1;
        private int _optCount = 1; private int _frmCount = 1;
        private int _lstCount = 1; private int _cmbCount = 1;
        private int _picCount = 1; private int _tmrCount = 1;
        private int _shpCount = 1; private int _linCount = 1;
        private int _scrCount = 1; private int _drvCount = 1;
        private int _dirCount = 1; private int _filCount = 1;
        private int _imgCount = 1;

        public MainWindow()
        {
            InitializeComponent();

            // Inicializar la capa de adornos cuando la ventana cargue
            this.Loaded += (s, e) => _adornerLayer = AdornerLayer.GetAdornerLayer(DesignCanvas);
        }

        #region Interactividad (Selección y Redimensionamiento)

        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectElement((UIElement)sender);
            _clickPosition = e.GetPosition(_selectedElement);
            _selectedElement.CaptureMouse();
            e.Handled = true;
        }

        private void SelectElement(UIElement element)
        {
            // 1. Limpiar selección anterior (quitar los puntos azules del control previo)
            if (_selectedElement != null) RemoveAdorner(_selectedElement);

            _selectedElement = element;

            // 2. Mostrar Adorner de redimensionamiento en el nuevo control
            ShowAdorner(_selectedElement);

            if (_selectedElement is FrameworkElement fe)
            {
                PropHeaderTitle.Text = "Propiedades - " + fe.Name;
                PropName.Text = fe.Name;

                if (fe is TextBox txt) { LabelPropCaption.Text = "Text"; PropCaption.Text = txt.Text; }
                else if (fe is GroupBox grp) { LabelPropCaption.Text = "Caption"; PropCaption.Text = grp.Header?.ToString(); }
                else if (fe is ContentControl cc) { LabelPropCaption.Text = "Caption"; PropCaption.Text = cc.Content?.ToString(); }
                else { LabelPropCaption.Text = "Caption"; PropCaption.Text = ""; }

                UpdatePositionProperties();
            }
        }

        // --- LÓGICA DE REDIMENSIONAMIENTO ---
        private void ShowAdorner(UIElement element)
        {
            if (_adornerLayer != null)
            {
                _currentAdorner = new ResizeAdorner(element);
                _adornerLayer.Add(_currentAdorner);
            }
        }

        private void RemoveAdorner(UIElement element)
        {
            if (_adornerLayer != null && _currentAdorner != null)
            {
                _adornerLayer.Remove(_currentAdorner);
                _currentAdorner = null;
            }
        }
        // ------------------------------------

        private void UpdatePositionProperties()
        {
            if (_selectedElement != null && _selectedElement is FrameworkElement fe)
            {
                double leftPx = Canvas.GetLeft(_selectedElement);
                double topPx = Canvas.GetTop(_selectedElement);
                PropLeft.Text = (Math.Round(leftPx) * PixelsToTwips).ToString();
                PropTop.Text = (Math.Round(topPx) * PixelsToTwips).ToString();
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selectedElement != null && _selectedElement.IsMouseCaptured)
            {
                Point currentMousePos = e.GetPosition(DesignCanvas);

                // Snap to grid (8px)
                double newLeft = Math.Round((currentMousePos.X - _clickPosition.X) / 8) * 8;
                double newTop = Math.Round((currentMousePos.Y - _clickPosition.Y) / 8) * 8;

                if (newLeft < 0) newLeft = 0;
                if (newTop < 0) newTop = 0;

                Canvas.SetLeft(_selectedElement, newLeft);
                Canvas.SetTop(_selectedElement, newTop);
                UpdatePositionProperties();
            }
            // Si solo movemos el mouse sobre el objeto seleccionado (sin arrastrar),
            // actualizamos las propiedades por si el Adorner cambió el tamaño
            else if (_selectedElement != null)
            {
                UpdatePositionProperties();
            }
        }

        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_selectedElement != null && _selectedElement.IsMouseCaptured)
                _selectedElement.ReleaseMouseCapture();
        }

        private void DesignCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Deseleccionar al hacer clic en el fondo y quitar Adorner
            if (_selectedElement != null) RemoveAdorner(_selectedElement);

            _selectedElement = null;
            PropHeaderTitle.Text = "Propiedades - Form1";
            PropName.Text = "Form1";
            PropCaption.Text = "Form1";
            PropLeft.Text = "0";
            PropTop.Text = "0";
        }

        #endregion

        #region Funciones de Archivo (Importar / Exportar)

        private void ImportFrm_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "VB6 Form (*.frm)|*.frm|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true) try { ParseVb6Frm(openFileDialog.FileName); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ExportCode_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "VB6 Form (*.frm)|*.frm";
            saveFileDialog.FileName = "Form1_New.frm";
            if (saveFileDialog.ShowDialog() == true) try { File.WriteAllText(saveFileDialog.FileName, GenerateVb6Code()); MessageBox.Show("Código generado."); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ExportImage_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PNG Image (*.png)|*.png";
            saveFileDialog.FileName = "Mockup.png";
            if (saveFileDialog.ShowDialog() == true) try { SaveCanvasToImage(saveFileDialog.FileName); MessageBox.Show("Imagen guardada."); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        #endregion

        #region Lógica de Parsing y Generación

        private void ParseVb6Frm(string filePath)
        {
            DesignCanvas.Children.Clear();
            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("Begin VB."))
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length < 3) continue;
                    string type = parts[1].Replace("VB.", "");
                    string name = parts[2];
                    CreateControlFromImport(type, name, lines, i);
                }
            }
        }

        private void CreateControlFromImport(string vbType, string name, string[] allLines, int startIndex)
        {
            FrameworkElement ctrl = null;
            switch (vbType)
            {
                case "CommandButton": ctrl = new Button(); break;
                case "Label": ctrl = new Label(); break;
                case "TextBox": ctrl = new TextBox { Background = Brushes.White }; break;
                case "CheckBox": ctrl = new CheckBox(); break;
                case "OptionButton": ctrl = new RadioButton(); break;
                case "Frame": ctrl = new GroupBox(); break;
                case "ComboBox": ctrl = new ComboBox(); break;
                case "ListBox": ctrl = new ListBox(); break;
                case "Timer": ctrl = CreateTimerVisual(); break;
                case "Shape": ctrl = new Rectangle { Stroke = Brushes.Black, StrokeDashArray = new DoubleCollection(new double[] { 2, 2 }) }; break;
                case "Image": ctrl = new Image { Width = 80, Height = 60, Stretch = Stretch.Uniform }; break;
                default: return;
            }

            ctrl.Name = name;
            double left = 0, top = 0, width = 100, height = 30;
            string caption = name;

            for (int j = startIndex + 1; j < allLines.Length; j++)
            {
                string l = allLines[j].Trim();
                if (l == "End") break;
                if (l.StartsWith("Begin ")) break;

                if (l.Contains("="))
                {
                    string prop = l.Split('=')[0].Trim();
                    string val = l.Split('=')[1].Trim().Replace("\"", "");

                    switch (prop)
                    {
                        case "Caption": case "Text": caption = val; break;
                        case "Left": left = double.Parse(val) / PixelsToTwips; break;
                        case "Top": top = double.Parse(val) / PixelsToTwips; break;
                        case "Width": width = double.Parse(val) / PixelsToTwips; break;
                        case "Height": height = double.Parse(val) / PixelsToTwips; break;
                    }
                }
            }

            ctrl.Width = width;
            ctrl.Height = height;

            if (ctrl is ContentControl cc) cc.Content = caption;
            if (ctrl is TextBox tb) tb.Text = caption;
            if (ctrl is GroupBox gb) gb.Header = caption;

            RegisterControl(ctrl);
            Canvas.SetLeft(ctrl, Math.Round(left / 8) * 8);
            Canvas.SetTop(ctrl, Math.Round(top / 8) * 8);
        }

        private string GenerateVb6Code()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("VERSION 5.00");
            sb.AppendLine("Begin VB.Form Form1");
            sb.AppendLine("   Caption         =   \"Mockup\"");
            sb.AppendLine($"   ClientHeight    =   {DesignCanvas.ActualHeight * PixelsToTwips}");
            sb.AppendLine($"   ClientWidth     =   {DesignCanvas.ActualWidth * PixelsToTwips}");

            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child is FrameworkElement fe)
                {
                    string vbType = MapWpfToVbType(fe);
                    if (string.IsNullOrEmpty(vbType)) continue;

                    sb.AppendLine($"   Begin VB.{vbType} {fe.Name}");
                    sb.AppendLine($"      Left            =   {Math.Round(Canvas.GetLeft(fe) * PixelsToTwips)}");
                    sb.AppendLine($"      Top             =   {Math.Round(Canvas.GetTop(fe) * PixelsToTwips)}");
                    sb.AppendLine($"      Width           =   {Math.Round(fe.Width * PixelsToTwips)}");
                    sb.AppendLine($"      Height          =   {Math.Round(fe.Height * PixelsToTwips)}");

                    if (fe is TextBox txt) sb.AppendLine($"      Text            =   \"{txt.Text}\"");
                    else if (fe is ContentControl cc) sb.AppendLine($"      Caption         =   \"{cc.Content}\"");
                    else if (fe is GroupBox gb) sb.AppendLine($"      Caption         =   \"{gb.Header}\"");

                    sb.AppendLine("   End");
                }
            }
            sb.AppendLine("End");
            return sb.ToString();
        }

        private string MapWpfToVbType(FrameworkElement fe)
        {
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
            return "";
        }

        private void SaveCanvasToImage(string fileName)
        {
            Size size = new Size(DesignCanvas.ActualWidth, DesignCanvas.ActualHeight);
            DesignCanvas.Measure(size);
            DesignCanvas.Arrange(new Rect(size));
            RenderTargetBitmap rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
            rtb.Render(DesignCanvas);

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                encoder.Save(fs);
            }
        }

        #endregion

        #region Toolbox (Creación de Controles)

        private void AddButton_Click(object sender, RoutedEventArgs e) => CreateControl<Button>("Command", ref _btnCount, c => { c.Content = c.Name; c.Width = 100; c.Height = 35; c.Background = GetVbGray(); c.BorderThickness = new Thickness(2); });
        private void AddLabel_Click(object sender, RoutedEventArgs e) => CreateControl<Label>("Label", ref _lblCount, c => { c.Content = c.Name; });
        private void AddTextBox_Click(object sender, RoutedEventArgs e) => CreateControl<TextBox>("Text", ref _txtCount, c => { c.Text = c.Name; c.Width = 100; c.Height = 24; c.Background = Brushes.White; });
        private void AddFrame_Click(object sender, RoutedEventArgs e) => CreateControl<GroupBox>("Frame", ref _frmCount, c => { c.Header = c.Name; c.Width = 150; c.Height = 100; });
        private void AddCheckBox_Click(object sender, RoutedEventArgs e) => CreateControl<CheckBox>("Check", ref _chkCount, c => { c.Content = c.Name; });
        private void AddOptionButton_Click(object sender, RoutedEventArgs e) => CreateControl<RadioButton>("Option", ref _optCount, c => { c.Content = c.Name; });
        private void AddComboBox_Click(object sender, RoutedEventArgs e) => CreateControl<ComboBox>("Combo", ref _cmbCount, c => { c.Width = 120; });
        private void AddListBox_Click(object sender, RoutedEventArgs e) => CreateControl<ListBox>("List", ref _lstCount, c => { c.Width = 100; c.Height = 80; });
        private void AddPictureBox_Click(object sender, RoutedEventArgs e) => CreateControl<Border>("Picture", ref _picCount, b => { b.Width = 100; b.Height = 80; b.Background = GetVbGray(); b.BorderBrush = Brushes.Black; b.BorderThickness = new Thickness(1); });
        private void AddTimer_Click(object sender, RoutedEventArgs e) => RegisterControl(CreateTimerVisual());
        private void AddHScrollBar_Click(object sender, RoutedEventArgs e) => CreateControl<ScrollBar>("HScroll", ref _scrCount, s => { s.Orientation = Orientation.Horizontal; s.Width = 120; s.Height = 18; });
        private void AddVScrollBar_Click(object sender, RoutedEventArgs e) => CreateControl<ScrollBar>("VScroll", ref _scrCount, s => { s.Orientation = Orientation.Vertical; s.Width = 18; s.Height = 120; });
        private void AddDriveListBox_Click(object sender, RoutedEventArgs e) => CreateControl<ComboBox>("Drive", ref _drvCount, c => { c.Width = 120; c.Text = "C: [Local]"; });
        private void AddDirListBox_Click(object sender, RoutedEventArgs e) => CreateControl<ListBox>("Dir", ref _dirCount, l => { l.Width = 120; l.Height = 80; });
        private void AddFileListBox_Click(object sender, RoutedEventArgs e) => CreateControl<ListBox>("File", ref _filCount, l => { l.Width = 120; l.Height = 80; });
        private void AddShape_Click(object sender, RoutedEventArgs e) => CreateControl<Rectangle>("Shape", ref _shpCount, r => { r.Width = 64; r.Height = 64; r.Stroke = Brushes.Black; r.StrokeThickness = 1; });
        private void AddLine_Click(object sender, RoutedEventArgs e) => CreateControl<Line>("Line", ref _linCount, l => { l.X1 = 0; l.Y1 = 0; l.X2 = 80; l.Y2 = 80; l.Stroke = Brushes.Black; l.StrokeThickness = 1; });
        private void AddImage_Click(object sender, RoutedEventArgs e) => CreateControl<Image>("Image", ref _imgCount, i => { i.Width = 80; i.Height = 60; i.Source = null; });

        private void CreateControl<T>(string prefix, ref int counter, Action<T> initialize) where T : FrameworkElement, new()
        {
            T ctrl = new T();
            ctrl.Name = prefix + counter;
            counter++;
            initialize(ctrl);
            RegisterControl(ctrl);
        }

        private void RegisterControl(FrameworkElement ctrl)
        {
            ctrl.PreviewMouseLeftButtonDown += Control_MouseLeftButtonDown;
            ctrl.PreviewMouseMove += Control_MouseMove;
            ctrl.PreviewMouseLeftButtonUp += Control_MouseLeftButtonUp;

            if (ctrl is Control c && !(ctrl is Label))
            {
                c.FontFamily = new FontFamily("MS Sans Serif");
                c.FontSize = 11;
            }

            DesignCanvas.Children.Add(ctrl);

            if (double.IsNaN(Canvas.GetLeft(ctrl))) Canvas.SetLeft(ctrl, 80);
            if (double.IsNaN(Canvas.GetTop(ctrl))) Canvas.SetTop(ctrl, 80);

            SelectElement(ctrl);
        }

        private Grid CreateTimerVisual()
        {
            Grid g = new Grid { Width = 32, Height = 32 };
            g.Children.Add(new Ellipse { Stroke = Brushes.Black, StrokeThickness = 1 });
            g.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M16,4 L16,16 L22,22"), Stroke = Brushes.Black, StrokeThickness = 1 });
            return g;
        }

        private Brush GetVbGray() => new SolidColorBrush(Color.FromRgb(212, 208, 200));

        private void PropCaption_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElement == null) return;
            if (_selectedElement is TextBox txt) txt.Text = PropCaption.Text;
            else if (_selectedElement is GroupBox grp) grp.Header = PropCaption.Text;
            else if (_selectedElement is ContentControl cc) cc.Content = PropCaption.Text;
        }

        private void PropName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElement is FrameworkElement fe)
            {
                fe.Name = PropName.Text;
                PropHeaderTitle.Text = "Propiedades - " + fe.Name;
            }
        }
        #endregion
    }

}