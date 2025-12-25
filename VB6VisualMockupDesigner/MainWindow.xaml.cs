using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        // Estado del editor
        private UIElement _selectedElement = null;
        private Point _clickPosition;
        private const int PixelsToTwips = 15; // Factor de conversión estándar de VB6

        // Contadores para nombres únicos de componentes
        private int _btnCount = 1; private int _lblCount = 1;
        private int _txtCount = 1; private int _chkCount = 1;
        private int _optCount = 1; private int _frmCount = 1;
        private int _lstCount = 1; private int _cmbCount = 1;
        private int _picCount = 1; private int _imgCount = 1;
        private int _tmrCount = 1; private int _shpCount = 1;
        private int _linCount = 1; private int _scrCount = 1;
        private int _drvCount = 1; private int _dirCount = 1;
        private int _filCount = 1;

        public MainWindow()
        {
            InitializeComponent();
        }

        #region Manejadores de la Toolbox

        private void AddButton_Click(object sender, RoutedEventArgs e) =>
            CreateControl<Button>("Command", ref _btnCount, c => {
                c.Content = c.Name; c.Width = 100; c.Height = 35;
                c.Background = GetVbGray(); c.BorderBrush = Brushes.Gray; c.BorderThickness = new Thickness(2);
            });

        private void AddLabel_Click(object sender, RoutedEventArgs e) =>
            CreateControl<Label>("Label", ref _lblCount, c => { c.Content = c.Name; c.Background = Brushes.Transparent; });

        private void AddTextBox_Click(object sender, RoutedEventArgs e) =>
            CreateControl<TextBox>("Text", ref _txtCount, c => {
                c.Text = c.Name; c.Width = 100; c.Height = 24;
                c.Background = Brushes.White; c.BorderBrush = Brushes.Black; c.BorderThickness = new Thickness(1);
            });

        private void AddFrame_Click(object sender, RoutedEventArgs e) =>
            CreateControl<GroupBox>("Frame", ref _frmCount, c => {
                c.Header = c.Name; c.Width = 150; c.Height = 100;
                c.BorderBrush = Brushes.Gray; c.BorderThickness = new Thickness(1);
            });

        private void AddCheckBox_Click(object sender, RoutedEventArgs e) =>
            CreateControl<CheckBox>("Check", ref _chkCount, c => { c.Content = c.Name; });

        private void AddOptionButton_Click(object sender, RoutedEventArgs e) =>
            CreateControl<RadioButton>("Option", ref _optCount, c => { c.Content = c.Name; });

        private void AddComboBox_Click(object sender, RoutedEventArgs e) =>
            CreateControl<ComboBox>("Combo", ref _cmbCount, c => { c.Width = 120; c.Height = 25; c.IsReadOnly = true; });

        private void AddListBox_Click(object sender, RoutedEventArgs e) =>
            CreateControl<ListBox>("List", ref _lstCount, c => { c.Width = 100; c.Height = 80; c.BorderBrush = Brushes.Black; c.BorderThickness = new Thickness(1); });

        private void AddPictureBox_Click(object sender, RoutedEventArgs e) =>
            CreateControl<Border>("Picture", ref _picCount, b => {
                b.Width = 100; b.Height = 80; b.Background = GetVbGray();
                b.BorderBrush = Brushes.Black; b.BorderThickness = new Thickness(1);
            });

        private void AddTimer_Click(object sender, RoutedEventArgs e) =>
            CreateControl<Grid>("Timer", ref _tmrCount, g => {
                g.Width = 32; g.Height = 32;
                var el = new Ellipse { Stroke = Brushes.Black, StrokeThickness = 1 };
                var path = new Path { Data = Geometry.Parse("M16,4 L16,16 L22,22"), Stroke = Brushes.Black, StrokeThickness = 1 };
                g.Children.Add(el); g.Children.Add(path);
            });

        private void AddHScrollBar_Click(object sender, RoutedEventArgs e) =>
            CreateControl<ScrollBar>("HScroll", ref _scrCount, s => { s.Orientation = Orientation.Horizontal; s.Width = 120; s.Height = 18; });

        private void AddVScrollBar_Click(object sender, RoutedEventArgs e) =>
            CreateControl<ScrollBar>("VScroll", ref _scrCount, s => { s.Orientation = Orientation.Vertical; s.Width = 18; s.Height = 120; });

        private void AddDriveListBox_Click(object sender, RoutedEventArgs e) =>
            CreateControl<ComboBox>("Drive", ref _drvCount, c => { c.Width = 120; c.Text = "C: [Local]"; });

        private void AddDirListBox_Click(object sender, RoutedEventArgs e) =>
            CreateControl<ListBox>("Dir", ref _dirCount, l => { l.Width = 120; l.Height = 80; });

        private void AddFileListBox_Click(object sender, RoutedEventArgs e) =>
            CreateControl<ListBox>("File", ref _filCount, l => { l.Width = 120; l.Height = 80; });

        private void AddShape_Click(object sender, RoutedEventArgs e) =>
            CreateControl<Rectangle>("Shape", ref _shpCount, r => { r.Width = 64; r.Height = 64; r.Stroke = Brushes.Black; r.StrokeThickness = 1; });

        private void AddLine_Click(object sender, RoutedEventArgs e) =>
            CreateControl<Line>("Line", ref _linCount, l => { l.X1 = 0; l.Y1 = 0; l.X2 = 80; l.Y2 = 80; l.Stroke = Brushes.Black; l.StrokeThickness = 1; });

        private void AddImage_Click(object sender, RoutedEventArgs e) =>
            CreateControl<Image>("Image", ref _imgCount, i => { i.Width = 80; i.Height = 60; i.Source = null; /* Placeholder */ });

        #endregion

        #region Lógica de Creación y Registro

        private void CreateControl<T>(string prefix, ref int counter, Action<T> initialize) where T : FrameworkElement, new()
        {
            T ctrl = new T();
            ctrl.Name = prefix + counter;
            counter++;

            initialize(ctrl);

            // Aplicar fuente estándar VB6 si el control lo permite
            if (ctrl is Control control)
            {
                control.FontFamily = new FontFamily("MS Sans Serif");
                control.FontSize = 11;
            }

            RegisterControl(ctrl);
        }

        private void RegisterControl(FrameworkElement ctrl)
        {
            // Suscribir eventos con Preview para prioridad sobre el comportamiento nativo
            ctrl.PreviewMouseLeftButtonDown += Control_MouseLeftButtonDown;
            ctrl.PreviewMouseMove += Control_MouseMove;
            ctrl.PreviewMouseLeftButtonUp += Control_MouseLeftButtonUp;

            DesignCanvas.Children.Add(ctrl);
            Canvas.SetLeft(ctrl, 80); // Posición inicial alineada a cuadrícula
            Canvas.SetTop(ctrl, 80);

            SelectElement(ctrl);
        }

        private Brush GetVbGray() => new SolidColorBrush(Color.FromRgb(212, 208, 200));

        #endregion

        #region Interacción del Canvas (Arrastre y Selección)

        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectElement((UIElement)sender);
            _clickPosition = e.GetPosition(_selectedElement);
            _selectedElement.CaptureMouse();
            e.Handled = true;
        }

        private void SelectElement(UIElement element)
        {
            _selectedElement = element;

            if (_selectedElement is FrameworkElement fe)
            {
                PropHeaderTitle.Text = "Properties - " + fe.Name;
                PropName.Text = fe.Name;

                // Actualizar etiqueta y valor de Caption/Text según tipo de control
                if (fe is TextBox txt)
                {
                    LabelPropCaption.Text = "Text";
                    PropCaption.Text = txt.Text;
                }
                else if (fe is GroupBox grp)
                {
                    LabelPropCaption.Text = "Caption";
                    PropCaption.Text = grp.Header?.ToString();
                }
                else if (fe is ContentControl cc)
                {
                    LabelPropCaption.Text = "Caption";
                    PropCaption.Text = cc.Content?.ToString();
                }
                else
                {
                    LabelPropCaption.Text = "Caption";
                    PropCaption.Text = "";
                }

                UpdatePositionProperties();
            }
        }

        private void UpdatePositionProperties()
        {
            if (_selectedElement != null)
            {
                double leftPx = Canvas.GetLeft(_selectedElement);
                double topPx = Canvas.GetTop(_selectedElement);

                // Mostrar en Twips (estándar VB6)
                PropLeft.Text = (Math.Round(leftPx) * PixelsToTwips).ToString();
                PropTop.Text = (Math.Round(topPx) * PixelsToTwips).ToString();
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selectedElement != null && _selectedElement.IsMouseCaptured)
            {
                Point currentMousePos = e.GetPosition(DesignCanvas);

                // Cálculo de nueva posición con Snap-to-Grid (8px)
                double newLeft = Math.Round((currentMousePos.X - _clickPosition.X) / 8) * 8;
                double newTop = Math.Round((currentMousePos.Y - _clickPosition.Y) / 8) * 8;

                // Restricción dentro del lienzo
                if (newLeft < 0) newLeft = 0;
                if (newTop < 0) newTop = 0;

                Canvas.SetLeft(_selectedElement, newLeft);
                Canvas.SetTop(_selectedElement, newTop);

                UpdatePositionProperties();
            }
        }

        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_selectedElement != null && _selectedElement.IsMouseCaptured)
            {
                _selectedElement.ReleaseMouseCapture();
            }
        }

        private void DesignCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Deseleccionar al hacer clic en el fondo
            _selectedElement = null;
            PropHeaderTitle.Text = "Properties - Form1";
            PropName.Text = "Form1";
            PropCaption.Text = "Form1";
            PropLeft.Text = "0";
            PropTop.Text = "0";
        }

        #endregion

        #region Sincronización de Propiedades

        private void PropCaption_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElement == null) return;

            if (_selectedElement is TextBox txt)
            {
                txt.Text = PropCaption.Text;
            }
            else if (_selectedElement is GroupBox grp)
            {
                grp.Header = PropCaption.Text;
            }
            else if (_selectedElement is ContentControl cc)
            {
                cc.Content = PropCaption.Text;
            }
        }

        private void PropName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElement is FrameworkElement fe)
            {
                fe.Name = PropName.Text;
                PropHeaderTitle.Text = "Properties - " + fe.Name;
            }
        }

        #endregion
    }

}