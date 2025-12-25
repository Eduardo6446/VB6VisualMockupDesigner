using System.Text;
using System.Windows;
using System.Windows.Controls;
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
        // Elemento actualmente seleccionado en el lienzo
        private UIElement _selectedElement = null;

        // Posición relativa del mouse dentro del control al iniciar el arrastre
        private Point _clickPosition;

        // Contador para generar nombres únicos (Command1, Command2, etc.)
        private int _controlCount = 1;

        // Constante de conversión: 1 píxel equivale aproximadamente a 15 twips en VB6
        private const int PixelsToTwips = 15;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Agrega un nuevo CommandButton al área de diseño con eventos de arrastre mejorados.
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Button newBtn = new Button
            {
                Name = "Command" + _controlCount,
                Content = "Command" + _controlCount,
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(Color.FromRgb(212, 208, 200)),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(2),
                FontFamily = new FontFamily("MS Sans Serif"),
                FontSize = 11
            };

            _controlCount++;

            // Usamos PreviewMouseLeftButtonDown para interceptar el clic antes que el comportamiento interno del botón
            newBtn.PreviewMouseLeftButtonDown += Control_MouseLeftButtonDown;
            newBtn.PreviewMouseMove += Control_MouseMove;
            newBtn.PreviewMouseLeftButtonUp += Control_MouseLeftButtonUp;

            // Posicionamiento inicial en el Canvas
            DesignCanvas.Children.Add(newBtn);
            Canvas.SetLeft(newBtn, 80);
            Canvas.SetTop(newBtn, 80);

            // Seleccionar automáticamente el nuevo control
            SelectElement(newBtn);
        }

        /// <summary>
        /// Maneja el clic inicial. Usamos Preview para asegurar que el arrastre comience siempre.
        /// </summary>
        private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectElement((UIElement)sender);

            _clickPosition = e.GetPosition(_selectedElement);
            _selectedElement.CaptureMouse();

            e.Handled = true; // Evita que el evento llegue a otros elementos inferiores
        }

        /// <summary>
        /// Actualiza la interfaz del panel de propiedades.
        /// </summary>
        private void SelectElement(UIElement element)
        {
            _selectedElement = element;

            if (_selectedElement is Button btn)
            {
                PropHeaderTitle.Text = "Properties - " + btn.Name;
                PropName.Text = btn.Name;
                PropCaption.Text = btn.Content.ToString();
                UpdatePositionProperties();
            }
        }

        /// <summary>
        /// Calcula y muestra la posición actual del control en Twips.
        /// </summary>
        private void UpdatePositionProperties()
        {
            if (_selectedElement != null)
            {
                double leftPx = Canvas.GetLeft(_selectedElement);
                double topPx = Canvas.GetTop(_selectedElement);

                PropLeft.Text = (Math.Round(leftPx) * PixelsToTwips).ToString();
                PropTop.Text = (Math.Round(topPx) * PixelsToTwips).ToString();
            }
        }

        /// <summary>
        /// Lógica de arrastre con Snap to Grid.
        /// </summary>
        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selectedElement != null && _selectedElement.IsMouseCaptured)
            {
                Point currentMousePos = e.GetPosition(DesignCanvas);

                double newLeft = currentMousePos.X - _clickPosition.X;
                double newTop = currentMousePos.Y - _clickPosition.Y;

                // Snap to grid (múltiplos de 8 píxeles para coincidir con los puntos del fondo)
                newLeft = Math.Round(newLeft / 8) * 8;
                newTop = Math.Round(newTop / 8) * 8;

                // Limitar el movimiento dentro de los bordes del Canvas
                if (newLeft < 0) newLeft = 0;
                if (newTop < 0) newTop = 0;
                if (newLeft + ((FrameworkElement)_selectedElement).ActualWidth > DesignCanvas.ActualWidth)
                    newLeft = DesignCanvas.ActualWidth - ((FrameworkElement)_selectedElement).ActualWidth;
                if (newTop + ((FrameworkElement)_selectedElement).ActualHeight > DesignCanvas.ActualHeight)
                    newTop = DesignCanvas.ActualHeight - ((FrameworkElement)_selectedElement).ActualHeight;

                Canvas.SetLeft(_selectedElement, newLeft);
                Canvas.SetTop(_selectedElement, newTop);

                UpdatePositionProperties();
            }
        }

        /// <summary>
        /// Finaliza la operación de arrastre liberando la captura del mouse.
        /// </summary>
        private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_selectedElement != null && _selectedElement.IsMouseCaptured)
            {
                _selectedElement.ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// Deselecciona elementos al hacer clic en el fondo.
        /// </summary>
        private void DesignCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _selectedElement = null;
            PropHeaderTitle.Text = "Properties - Form1";
            PropName.Text = "Form1";
            PropCaption.Text = "Form1";
            PropLeft.Text = "0";
            PropTop.Text = "0";
        }

        // --- Eventos para edición desde el Panel de Propiedades ---

        private void PropCaption_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElement is Button btn)
            {
                btn.Content = PropCaption.Text;
            }
        }

        private void PropName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedElement is Button btn)
            {
                btn.Name = PropName.Text;
                PropHeaderTitle.Text = "Properties - " + btn.Name;
            }
        }
    }


}