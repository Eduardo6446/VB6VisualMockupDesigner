using System;
using System.Collections.Generic;
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
    /// Interaction logic for DesignerCanvas.xaml
    /// </summary>
    public partial class DesignerCanvas : UserControl
    {

        // VARIABLES DE ESTADO PARA ARRASTRE
        private bool _isDragging = false;
        private Point _clickOffset;       // Dónde hice clic dentro del control
        private UIElement _selectedControl; // El control que tengo seleccionado actualmente

        // CAPA VISUAL DE SELECCIÓN (Simularemos los 8 cuadraditos más tarde)
        private Border _selectionBorder;

        // VARIABLES PARA REDIMENSIÓN DE VENTANA
        private bool _isResizingForm = false;
        private Point _resizeClickStart;
        private double _initialFormWidth;
        private double _initialFormHeight;

        private void ResizeGrip_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var grip = sender as UIElement;
            _isResizingForm = true;
            _resizeClickStart = e.GetPosition(this); // Posición relativa al UserControl completo

            // Guardamos tamaño actual
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

                // Calcular delta
                double deltaX = currentPos.X - _resizeClickStart.X;
                double deltaY = currentPos.Y - _resizeClickStart.Y;

                // Calcular nuevo tamaño (con SnapToGrid opcional para la ventana también)
                double newWidth = _initialFormWidth + deltaX;
                double newHeight = _initialFormHeight + deltaY;

                // Limites mínimos (para que no desaparezca la ventana)
                if (newWidth < 100) newWidth = 100;
                if (newHeight < 100) newHeight = 100;

                // Aplicar al Grid contenedor (y el Border se estirará para llenarlo)
                WindowResizerGrid.Width = SnapToGrid(newWidth);
                WindowResizerGrid.Height = SnapToGrid(newHeight);
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

        public DesignerCanvas()
        {
            InitializeComponent();

            this.Loaded += DesignerCanvas_Loaded;
        }

        private void DesignerCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            CenterView();
            // Nos desuscribimos para que solo ocurra la primera vez que se muestra
            this.Loaded -= DesignerCanvas_Loaded;
        }

        // NUEVO MÉTODO: Calcula el centro y mueve los scrollbars
        private void CenterView()
        {
            // Asegurarnos de que los controles existen y tienen tamaño
            if (MainScrollViewer == null || DesignGrid == null || MainScrollViewer.ViewportWidth == 0) return;

            // Cálculo del centro: (AnchoTotal / 2) - (AnchoVisible / 2)
            double horizontalOffset = (DesignGrid.Width / 2) - (MainScrollViewer.ViewportWidth / 2);
            double verticalOffset = (DesignGrid.Height / 2) - (MainScrollViewer.ViewportHeight / 2);

            // Aplicar el desplazamiento
            MainScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            MainScrollViewer.ScrollToVerticalOffset(verticalOffset);
        }
        public void ClearCanvas()
        {
            DesignSurface.Children.Clear();
            // Opcional: Resetear tamaño por defecto
            RetroFormContainer.Width = 480;
            RetroFormContainer.Height = 360;
        }

        // 2. Método para redimensionar la "Ventana VB6" (El borde gris)
        public void SetFormDimensions(double width, double height)
        {
            // En VB6 ClientWidth/Height son Twips. Aquí recibiremos Píxeles.
            // Sumamos un poco para bordes y título simulados
            RetroFormContainer.Width = width + 10;
            RetroFormContainer.Height = height + 30;
        }

        public void AddControlToCanvas(UIElement control, double x, double y)
        {
            if (control == null) return;

            // Ajustar a rejilla
            double snappedX = SnapToGrid(x);
            double snappedY = SnapToGrid(y);

            Canvas.SetLeft(control, snappedX);
            Canvas.SetTop(control, snappedY);

            DesignSurface.Children.Add(control);
        }




        // Propiedad para establecer el título del formulario simulado
        public string FormTitle
        {
            get { return FormTitleText.Text; }
            set { FormTitleText.Text = value; }
        }

        // Método para obtener el Canvas donde agregaremos controles
        public Canvas GetDesignSurface()
        {
            return DesignSurface;
        }

        public event EventHandler<FrameworkElement> ControlSelected;

        // Deseleccionar al hacer clic fuera (opcional para el futuro)
        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Aquí implementarás la lógica para quitar selección de controles
            // Keyboard.ClearFocus();
        }

        private void DesignSurface_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ControlToolboxItem"))
            {
                string controlType = e.Data.GetData("ControlToolboxItem") as string;
                Point dropPosition = e.GetPosition(DesignSurface);

                // Crear el control visual
                UIElement newControl = CreateRetroControl(controlType);

                if (newControl != null)
                {
                    // Posicionar en el Canvas
                    Canvas.SetLeft(newControl, SnapToGrid(dropPosition.X));
                    Canvas.SetTop(newControl, SnapToGrid(dropPosition.Y));

                    DesignSurface.Children.Add(newControl);
                }
            }
        }

        // AYUDA: Ajustar a la rejilla de 8px (Típico VB6)
        private double SnapToGrid(double val)
        {
            return Math.Round(val / 8.0) * 8.0;
        }

        // FÁBRICA DE CONTROLES RETRO (Simulación Visual Completa)
        public UIElement CreateRetroControl(string type)
        {
            Control control = null;
            FrameworkElement element = null; // Usamos FrameworkElement para abarcar Shapes y Controles

            // Estilos comunes (para no repetir tanto código)
            var fontParams = new { Family = new FontFamily("Microsoft Sans Serif"), Size = 11.0 };
            var vbGray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8"));
            var vbWhite = Brushes.White;
            var vbBlack = Brushes.Black;

            switch (type)
            {
                // --- POINTER (No crea control) ---
                case "Pointer":
                    return null;

                // --- STANDARD CONTROLS ---
                case "PictureBox":
                    // PictureBox en VB6 es un contenedor con borde 3D
                    var picBorder = new Border
                    {
                        Width = 100,
                        Height = 100,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(2),
                        // Simulación de borde 'Sunken' (Hundido)
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        { ShadowDepth = 0, BlurRadius = 0 }
                    };
                    element = picBorder;
                    break;

                case "Label":
                    control = new Label
                    {
                        Content = "Label1",
                        Width = 121,
                        Height = 25,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        Padding = new Thickness(2)
                    };
                    break;

                case "TextBox":
                    control = new TextBox
                    {
                        Text = "Text1",
                        Width = 121,
                        Height = 25,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1)
                    };
                    break;

                case "Frame":
                    // El GroupBox de WPF es el equivalente al Frame
                    control = new GroupBox
                    {
                        Header = "Frame1",
                        Width = 150,
                        Height = 100,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray
                    };
                    break;

                case "CommandButton":
                    control = new Button
                    {
                        Content = "Command1",
                        Width = 121,
                        Height = 33,
                        Background = vbGray,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size
                    };
                    break;

                case "CheckBox":
                    control = new CheckBox
                    {
                        Content = "Check1",
                        Width = 121,
                        Height = 25,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    break;

                case "OptionButton": // RadioButton en .NET
                    control = new RadioButton
                    {
                        Content = "Option1",
                        Width = 121,
                        Height = 25,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    break;

                case "ComboBox":
                    var combo = new ComboBox
                    {
                        Width = 121,
                        Height = 21,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        IsEditable = true,
                        Text = "Combo1"
                    };
                    control = combo;
                    break;

                case "ListBox":
                    var list = new ListBox
                    {
                        Width = 121,
                        Height = 100,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1)
                    };
                    list.Items.Add("List1"); // Item de muestra
                    control = list;
                    break;

                case "HScrollBar":
                    control = new System.Windows.Controls.Primitives.ScrollBar
                    {
                        Orientation = Orientation.Horizontal,
                        Width = 100,
                        Height = 17,
                        Value = 50,
                        Maximum = 100
                    };
                    break;

                case "VScrollBar":
                    control = new System.Windows.Controls.Primitives.ScrollBar
                    {
                        Orientation = Orientation.Vertical,
                        Width = 17,
                        Height = 100,
                        Value = 50,
                        Maximum = 100
                    };
                    break;

                // --- SYSTEM / FILE CONTROLS ---
                case "Timer":
                    // El Timer es invisible en runtime, pero visual en diseño.
                    // Usaremos un borde con un texto pequeño o imagen.
                    var timerBorder = new Border
                    {
                        Width = 34,
                        Height = 34,
                        Background = vbGray,
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock { Text = "Timer", FontSize = 8, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center }
                    };
                    element = timerBorder;
                    break;

                case "DriveListBox":
                    var driveCombo = new ComboBox
                    {
                        Width = 121,
                        Height = 21,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        Text = @"c: [OS]"
                    };
                    control = driveCombo;
                    break;

                case "DirListBox":
                    var dirList = new ListBox
                    {
                        Width = 121,
                        Height = 100,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        BorderBrush = Brushes.Gray
                    };
                    dirList.Items.Add(@"c:\");
                    dirList.Items.Add(@"  Windows");
                    dirList.Items.Add(@"    System32");
                    control = dirList;
                    break;

                case "FileListBox":
                    var fileList = new ListBox
                    {
                        Width = 121,
                        Height = 100,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        BorderBrush = Brushes.Gray
                    };
                    fileList.Items.Add("archivo1.txt");
                    fileList.Items.Add("project.vbp");
                    control = fileList;
                    break;

                // --- GRAPHICAL CONTROLS ---
                case "Shape":
                    // Simulamos el Shape default (Rectángulo)
                    var shape = new System.Windows.Shapes.Rectangle
                    {
                        Width = 50,
                        Height = 50,
                        Stroke = vbBlack,
                        StrokeThickness = 1,
                        Fill = Brushes.Transparent
                    };
                    element = shape;
                    break;

                case "Line":
                    // Line es difícil de manejar con Width/Height estándar, 
                    // simularemos una línea horizontal básica dentro de un canvas o caja
                    var line = new System.Windows.Shapes.Rectangle
                    {
                        Width = 100,
                        Height = 2,
                        Fill = vbBlack
                    };
                    element = line;
                    break;

                case "Image":
                    // En WPF, 'Border' no soporta líneas punteadas. 
                    // Usamos un Grid que contiene un Rectangle (que sí soporta StrokeDashArray)
                    var imgGrid = new Grid
                    {
                        Width = 100,
                        Height = 100,
                        Background = Brushes.Transparent // Necesario para detectar clics dentro
                    };

                    // 1. El borde punteado
                    var dashedRect = new System.Windows.Shapes.Rectangle
                    {
                        Stroke = Brushes.Gray,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection() { 4, 2 }, // Patrón punteado
                        Fill = Brushes.Transparent,
                        IsHitTestVisible = false // Dejamos que el Grid capture el mouse
                    };

                    // 2. El texto "Image" centrado
                    var imgText = new TextBlock
                    {
                        Text = "Image",
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false
                    };

                    imgGrid.Children.Add(dashedRect);
                    imgGrid.Children.Add(imgText);

                    element = imgGrid;
                    break;

                // --- DATA ---
                case "Data":
                    // Control Data (DAO) - Botones de navegación
                    var dataGrid = new Grid { Width = 150, Height = 25, Background = vbGray };
                    dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) }); // <|
                    dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) }); // <
                    dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Label
                    dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) }); // >
                    dataGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) }); // |>

                    // Simular botones (simplificado con bordes)
                    var btn1 = new Border { Background = vbGray, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
                    var btn2 = new Border { Background = vbGray, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
                    var lblData = new TextBlock { Text = "Data1", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

                    Grid.SetColumn(btn1, 0); dataGrid.Children.Add(btn1);
                    Grid.SetColumn(lblData, 2); dataGrid.Children.Add(lblData);

                    element = dataGrid;
                    break;

                case "OLE":
                    var oleBorder = new Border
                    {
                        Width = 75,
                        Height = 75,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        Background = Brushes.LightGray,
                        Child = new TextBlock { Text = "OLE", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
                    };
                    element = oleBorder;
                    break;

                default:
                    // Fallback
                    control = new Button { Content = type, Width = 80, Height = 30, Background = Brushes.Red };
                    break;
            }

            // Unificación de referencias
            if (control != null && element == null)
            {
                element = control;
            }

            // Configuración común de eventos y cursores
            if (element != null)
            {
                element.PreviewMouseDown += Control_PreviewMouseDown;
                element.PreviewMouseMove += Control_PreviewMouseMove;
                element.PreviewMouseUp += Control_PreviewMouseUp;
                element.Cursor = Cursors.SizeAll;

                // Asignar Tag para identificar tipo luego (útil para propiedades)
                element.Tag = type;
            }

            return element;
        }

        private void ShowSelectionIndicator(UIElement control)
        {
            // 1. Si ya había una selección, la quitamos
            if (_selectionBorder != null)
            {
                DesignSurface.Children.Remove(_selectionBorder);
                _selectionBorder = null;
            }

            if (control == null) return;

            // 2. Crear un borde visual alrededor del control
            // En el futuro, aquí es donde dibujaríamos los 8 cuadraditos blancos de VB6
            _selectionBorder = new Border
            {
                BorderBrush = Brushes.Blue, // Azul moderno para indicar selección
                BorderThickness = new Thickness(1),
                Width = ((FrameworkElement)control).Width + 6,  // Un poco más grande que el control
                Height = ((FrameworkElement)control).Height + 6,
                IsHitTestVisible = false // Importante: Que el clic pase a través de él
            };

            // 3. Posicionar el borde sobre el control
            double left = Canvas.GetLeft(control);
            double top = Canvas.GetTop(control);

            Canvas.SetLeft(_selectionBorder, left - 3);
            Canvas.SetTop(_selectionBorder, top - 3);

            // 4. Agregar al Canvas
            DesignSurface.Children.Add(_selectionBorder);
        }


        // 1. INICIAR ARRASTRE (Al hacer clic)
        private void Control_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var control = sender as UIElement;

            // Guardamos referencia y estado
            _selectedControl = control;
            _isDragging = true;

            // Calculamos dónde hicimos clic RELATIVO al control (para que no salte al moverlo)
            _clickOffset = e.GetPosition(control);

            // Capturamos el mouse para que no se pierda si lo mueves muy rápido fuera del control
            control.CaptureMouse();

            // Mostrar visualmente que está seleccionado
            ShowSelectionIndicator(control);

            // Detenemos la propagación para que no seleccione el fondo
            e.Handled = true;

            ControlSelected?.Invoke(this, control as FrameworkElement);
        
        }

        // 2. MOVER (Mientras arrastras)
        private void Control_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _selectedControl != null)
            {
                // 1. Calcular posición cruda
                Point currentPos = e.GetPosition(DesignSurface);
                double newLeft = currentPos.X - _clickOffset.X;
                double newTop = currentPos.Y - _clickOffset.Y;

                // 2. Snap to Grid
                newLeft = SnapToGrid(newLeft);
                newTop = SnapToGrid(newTop);

                // 3. Obtener dimensiones para calcular límites
                var frameworkControl = _selectedControl as FrameworkElement;
                double controlWidth = frameworkControl.ActualWidth;
                double controlHeight = frameworkControl.ActualHeight;

                // Limite derecho e inferior del contenedor (Canvas)
                double maxLeft = DesignSurface.ActualWidth - controlWidth;
                double maxTop = DesignSurface.ActualHeight - controlHeight;

                // 4. Aplicar Restricciones (Clamping)
                // Izquierda
                if (newLeft < 0) newLeft = 0;
                // Arriba
                if (newTop < 0) newTop = 0;
                // Derecha (Solo si el control cabe, si es más grande que el canvas, permitimos 0)
                if (newLeft > maxLeft && maxLeft > 0) newLeft = maxLeft;
                // Abajo
                if (newTop > maxTop && maxTop > 0) newTop = maxTop;

                // 5. Mover el control
                Canvas.SetLeft(_selectedControl, newLeft);
                Canvas.SetTop(_selectedControl, newTop);

                // 6. Mover el borde de selección
                if (_selectionBorder != null)
                {
                    Canvas.SetLeft(_selectionBorder, newLeft - 3);
                    Canvas.SetTop(_selectionBorder, newTop - 3);
                }
            }
        }

        // 3. TERMINAR ARRASTRE (Al soltar)
        private void Control_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;

                if (_selectedControl != null)
                {
                    _selectedControl.ReleaseMouseCapture();
                }
            }
        }

        private void DesignSurface_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Si hago clic en el vacío, quito la selección
            ShowSelectionIndicator(null);
            _selectedControl = null;

            ControlSelected?.Invoke(this, null);
        }


    }


}
