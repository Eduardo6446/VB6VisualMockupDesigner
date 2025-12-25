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
        // Lista de elementos seleccionados (Soporte para selección múltiple)
        private List<UIElement> _selectedElements = new List<UIElement>();

        // Variables para el arrastre de objetos
        private bool _isDragging = false;
        private Point _startClickPoint;
        private Dictionary<UIElement, Point> _initialPositions = new Dictionary<UIElement, Point>();

        // Variables para el Lazo de Selección (Rubber Band)
        private bool _isSelecting = false;
        private Point _selectionStartPoint;

        private const int PixelsToTwips = 15;
        private AdornerLayer _adornerLayer;

        // Contadores para nombres únicos
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

            // Cargar icono con seguridad (Intenta archivo, si falla usa generado)
            try
            {
                this.Icon = new BitmapImage(new Uri("pack://application:,,,/Resource/icon.ico"));
            }
            catch
            {
                this.Icon = CreateAppIcon();
            }

            // Inicializar capa de adornos
            this.Loaded += (s, e) => _adornerLayer = AdornerLayer.GetAdornerLayer(DesignCanvas);
        }

        #region Interactividad (Selección Múltiple, Arrastre y Lazo)

        // 1. Clic en un CONTROL (Inicio de selección o arrastre)
        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var clickedElement = (UIElement)sender;

            // Manejo de la tecla Ctrl para selección individual/múltiple
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                if (_selectedElements.Contains(clickedElement))
                {
                    // Si ya estaba seleccionado y presionamos Ctrl, lo quitamos
                    RemoveFromSelection(clickedElement);
                    e.Handled = true;
                    return; // No iniciamos arrastre si estamos deseleccionando
                }
                else
                {
                    // Si no estaba, lo agregamos al grupo existente
                    AddToSelection(clickedElement);
                }
            }
            else
            {
                // Comportamiento estándar (sin Ctrl):
                // Si hacemos clic en un objeto que NO es parte de la selección actual,
                // limpiamos todo y seleccionamos solo ese nuevo objeto.
                if (!_selectedElements.Contains(clickedElement))
                {
                    ClearSelection();
                    AddToSelection(clickedElement);
                }
                // Si el objeto YA es parte de una selección múltiple, no hacemos nada aquí
                // para permitir arrastrar el grupo completo sin perder la selección.
            }

            // Preparar arrastre para TODOS los elementos seleccionados
            _isDragging = true;
            _startClickPoint = e.GetPosition(DesignCanvas);
            _initialPositions.Clear();

            foreach (var el in _selectedElements)
            {
                _initialPositions[el] = new Point(Canvas.GetLeft(el), Canvas.GetTop(el));
            }

            // Capturar el mouse para asegurar el arrastre fluido
            clickedElement.CaptureMouse();
            e.Handled = true;
        }

        // 2. Clic en el CANVAS (Inicio de Lazo de Selección o Deselección)
        private void DesignCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Limpiar selección al hacer clic en el fondo
            ClearSelection();

            // Iniciar Lazo de Selección
            _isSelecting = true;
            _selectionStartPoint = e.GetPosition(DesignCanvas);

            // Configurar visualmente el rectángulo de selección
            Canvas.SetLeft(SelectionBox, _selectionStartPoint.X);
            Canvas.SetTop(SelectionBox, _selectionStartPoint.Y);
            SelectionBox.Width = 0;
            SelectionBox.Height = 0;
            SelectionBox.Visibility = Visibility.Visible;

            DesignCanvas.CaptureMouse();
        }

        // 3. Movimiento del Mouse (Arrastrando objetos O Arrastrando Lazo)
        private void DesignCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // A. Arrastrando objetos
            if (_isDragging)
            {
                Point currentMousePos = e.GetPosition(DesignCanvas);
                double deltaX = currentMousePos.X - _startClickPoint.X;
                double deltaY = currentMousePos.Y - _startClickPoint.Y;

                foreach (var el in _selectedElements)
                {
                    if (_initialPositions.ContainsKey(el))
                    {
                        Point initPos = _initialPositions[el];
                        double newLeft = initPos.X + deltaX;
                        double newTop = initPos.Y + deltaY;

                        // Snap to Grid (8px)
                        newLeft = Math.Round(newLeft / 8) * 8;
                        newTop = Math.Round(newTop / 8) * 8;

                        if (newLeft < 0) newLeft = 0;
                        if (newTop < 0) newTop = 0;

                        Canvas.SetLeft(el, newLeft);
                        Canvas.SetTop(el, newTop);
                    }
                }
                UpdatePropertyPanel();
                return;
            }

            // B. Arrastrando Lazo de Selección
            if (_isSelecting)
            {
                Point currentPos = e.GetPosition(DesignCanvas);

                double x = Math.Min(currentPos.X, _selectionStartPoint.X);
                double y = Math.Min(currentPos.Y, _selectionStartPoint.Y);
                double w = Math.Abs(currentPos.X - _selectionStartPoint.X);
                double h = Math.Abs(currentPos.Y - _selectionStartPoint.Y);

                Canvas.SetLeft(SelectionBox, x);
                Canvas.SetTop(SelectionBox, y);
                SelectionBox.Width = w;
                SelectionBox.Height = h;
            }
        }

        // 4. Soltar Mouse
        private void DesignCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                foreach (var el in _selectedElements) el.ReleaseMouseCapture();
            }

            if (_isSelecting)
            {
                _isSelecting = false;
                SelectionBox.Visibility = Visibility.Collapsed;
                DesignCanvas.ReleaseMouseCapture();

                // Seleccionar elementos dentro del rectángulo
                Rect selectionRect = new Rect(Canvas.GetLeft(SelectionBox), Canvas.GetTop(SelectionBox), SelectionBox.Width, SelectionBox.Height);
                SelectControlsInRect(selectionRect);
            }
        }

        // Redirecciones para eventos de controles individuales
        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging) DesignCanvas_MouseMove(sender, e);
        }

        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging) DesignCanvas_MouseUp(sender, e);
        }

        #endregion

        #region Gestión de Selección

        private void ClearSelection()
        {
            if (_adornerLayer == null) return;

            // Quitar adorners de los elementos seleccionados
            foreach (var el in _selectedElements)
            {
                var adorners = _adornerLayer.GetAdorners(el);
                if (adorners != null)
                {
                    foreach (var ad in adorners) _adornerLayer.Remove(ad);
                }
            }
            _selectedElements.Clear();

            // Resetear panel propiedades usando el actualizador
            UpdatePropertyPanel();
        }

        private void AddToSelection(UIElement element)
        {
            if (!_selectedElements.Contains(element))
            {
                _selectedElements.Add(element);
                if (_adornerLayer != null)
                {
                    _adornerLayer.Add(new ResizeAdorner(element));
                }
            }
            UpdatePropertyPanel();
        }

        private void RemoveFromSelection(UIElement element)
        {
            if (_selectedElements.Contains(element))
            {
                _selectedElements.Remove(element);
                if (_adornerLayer != null)
                {
                    var adorners = _adornerLayer.GetAdorners(element);
                    if (adorners != null)
                    {
                        foreach (var ad in adorners) _adornerLayer.Remove(ad);
                    }
                }
            }
            UpdatePropertyPanel();
        }

        private void SelectControlsInRect(Rect rect)
        {
            foreach (UIElement child in DesignCanvas.Children)
            {
                if (child == SelectionBox) continue;
                if (!(child is FrameworkElement fe)) continue;

                double left = Canvas.GetLeft(fe);
                double top = Canvas.GetTop(fe);
                Rect childRect = new Rect(left, top, fe.Width, fe.Height);

                if (rect.IntersectsWith(childRect))
                {
                    AddToSelection(fe);
                }
            }
        }

        private void UpdatePropertyPanel()
        {
            if (_selectedElements.Count == 0)
            {
                PropHeaderTitle.Text = "Properties - Form1";
                PropName.Text = "Form1";
                PropCaption.Text = "Form1";
                PropLeft.Text = "0";
                PropTop.Text = "0";
            }
            else if (_selectedElements.Count == 1)
            {
                var el = _selectedElements[0] as FrameworkElement;
                if (el != null)
                {
                    PropHeaderTitle.Text = "Propiedades - " + el.Name;
                    PropName.Text = el.Name;

                    double leftPx = Canvas.GetLeft(el);
                    double topPx = Canvas.GetTop(el);
                    PropLeft.Text = (Math.Round(leftPx) * PixelsToTwips).ToString();
                    PropTop.Text = (Math.Round(topPx) * PixelsToTwips).ToString();

                    if (el is TextBox txt) { LabelPropCaption.Text = "Text"; PropCaption.Text = txt.Text; }
                    else if (el is GroupBox grp) { LabelPropCaption.Text = "Caption"; PropCaption.Text = grp.Header?.ToString(); }
                    else if (el is ContentControl cc) { LabelPropCaption.Text = "Caption"; PropCaption.Text = cc.Content?.ToString(); }
                }
            }
            else if (_selectedElements.Count > 1)
            {
                PropHeaderTitle.Text = "Varios elementos seleccionados";
                PropName.Text = "";
                PropCaption.Text = "";
                PropLeft.Text = "";
                PropTop.Text = "";
            }
        }

        #endregion

        #region Generación de Icono (Respaldo)
        private ImageSource CreateAppIcon()
        {
            DrawingGroup drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(0, 50, 120)), new Pen(Brushes.White, 1), new RectangleGeometry(new Rect(0, 0, 32, 32), 3, 3)));

            FormattedText text = new FormattedText("VB", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal), 18, Brushes.White, 96);
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.White, null, text.BuildGeometry(new Point(3, 5))));

            // Se usa Geometry base para evitar errores de conversión implícita
            Geometry body = Geometry.Parse("M 20,20 L 28,12 L 30,14 L 22,22 Z");
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.Gold, new Pen(Brushes.Black, 0.5), body));

            Geometry eraser = Geometry.Parse("M 28,12 L 30,14 L 32,12 L 30,10 Z");
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.HotPink, new Pen(Brushes.Black, 0.5), eraser));

            Geometry tip = Geometry.Parse("M 20,20 L 22,22 L 18,24 Z");
            drawingGroup.Children.Add(new GeometryDrawing(Brushes.Black, null, tip));

            return new DrawingImage(drawingGroup);
        }
        #endregion

        #region Funciones de Archivo
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

        #region Parsing y Generación
        private void ParseVb6Frm(string filePath)
        {
            DesignCanvas.Children.Clear();
            DesignCanvas.Children.Add(SelectionBox); // Re-agregar el cuadro de selección

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
            ctrl.Width = width; ctrl.Height = height;
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
                if (child is FrameworkElement fe && child != SelectionBox)
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
            SelectionBox.Visibility = Visibility.Collapsed;
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

        #region Toolbox & Helpers
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
            T ctrl = new T(); ctrl.Name = prefix + counter; counter++; initialize(ctrl); RegisterControl(ctrl);
        }
        private void RegisterControl(FrameworkElement ctrl)
        {
            ctrl.PreviewMouseLeftButtonDown += Control_MouseLeftButtonDown;
            ctrl.PreviewMouseMove += Control_MouseMove;
            ctrl.PreviewMouseLeftButtonUp += Control_MouseLeftButtonUp;
            if (ctrl is Control c && !(ctrl is Label)) { c.FontFamily = new FontFamily("MS Sans Serif"); c.FontSize = 11; }
            DesignCanvas.Children.Add(ctrl);
            if (double.IsNaN(Canvas.GetLeft(ctrl))) Canvas.SetLeft(ctrl, 80);
            if (double.IsNaN(Canvas.GetTop(ctrl))) Canvas.SetTop(ctrl, 80);
            ClearSelection(); AddToSelection(ctrl);
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
            if (_selectedElements.Count == 1)
            {
                var el = _selectedElements[0];
                if (el is TextBox txt) txt.Text = PropCaption.Text;
                else if (el is GroupBox grp) grp.Header = PropCaption.Text;
                else if (el is ContentControl cc) cc.Content = PropCaption.Text;
            }
        }
        private void PropName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElements.Count == 1 && _selectedElements[0] is FrameworkElement fe)
            {
                fe.Name = PropName.Text; PropHeaderTitle.Text = "Propiedades - " + fe.Name;
            }
        }
        #endregion
    }

}