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

        public class VbControlModel
        {
            public string Type { get; set; }        // Ej: VB.PictureBox
            public string Name { get; set; }        // Ej: picBoxMain
            public int Index { get; set; } = -1;    // Para arrays de controles
            public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
            public List<VbControlModel> Children { get; set; } = new List<VbControlModel>();
            public VbControlModel Parent { get; set; }
        }

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
        // Método para redimensionar la "Ventana VB6" simulada
        public void SetFormDimensions(double width, double height)
        {
            // Validaciones para evitar crashes con tamaños inválidos
            if (width < 100) width = 100;
            if (height < 100) height = 100;

            // Redimensionamos el Grid contenedor (que contiene los Grips de redimensión)
            if (WindowResizerGrid != null)
            {
                WindowResizerGrid.Width = width;
                WindowResizerGrid.Height = height;
            }

            // Si usas el Borde interno (RetroFormContainer), asegúrate que se ajuste o herede
            if (RetroFormContainer != null)
            {
                RetroFormContainer.Width = width;
                RetroFormContainer.Height = height;
            }
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

        public VbControlModel ParseVb6Form(string fileContent)
        {
            var lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            VbControlModel root = null;
            VbControlModel current = null;
            Stack<VbControlModel> stack = new Stack<VbControlModel>();

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 1. Detectar Inicio de Control
                if (line.StartsWith("Begin "))
                {
                    // Formato: Begin Libreria.Tipo Nombre 
                    var parts = line.Substring(6).Split(' ');
                    string type = parts[0];
                    string name = parts.Length > 1 ? parts[1] : type;

                    var newControl = new VbControlModel { Type = type, Name = name, Parent = current };

                    if (current != null)
                    {
                        current.Children.Add(newControl);
                    }
                    else
                    {
                        root = newControl; // El primer Begin es el Form
                    }

                    current = newControl;
                    stack.Push(current);
                }
                // 2. Detectar Fin de Bloque
                else if (line == "End")
                {
                    if (stack.Count > 0)
                    {
                        stack.Pop();
                        current = stack.Count > 0 ? stack.Peek() : null;
                    }
                }
                // 3. Propiedades
                else if (line.Contains("=") && current != null)
                {
                    var eqIndex = line.IndexOf('=');
                    string propName = line.Substring(0, eqIndex).Trim();
                    string propValue = line.Substring(eqIndex + 1).Trim();

                    // Limpieza básica de comillas y comentarios
                    if (propValue.Contains("'")) propValue = propValue.Substring(0, propValue.IndexOf("'")).Trim();
                    propValue = propValue.Replace("\"", "");

                    if (!current.Properties.ContainsKey(propName))
                    {
                        current.Properties.Add(propName, propValue);
                    }
                }
            }
            return root;
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
                    // PictureBox ahora es un Canvas dentro de un Border
                    var picBorder = new Border
                    {
                        Width = 100,
                        Height = 100,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(2),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect { ShadowDepth = 0, BlurRadius = 0 } // Sunken effect
                    };
                    // IMPORTANTE: El hijo es un Canvas para poder meterle cosas adentro
                    var picCanvas = new Canvas();
                    picBorder.Child = picCanvas;

                    // Le ponemos nombre al Canvas para recuperarlo luego o usamos el Border
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
                    var grp = new GroupBox
                    {
                        Header = "Frame1",
                        Width = 150,
                        Height = 100,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray
                    };
                    // El GroupBox necesita un Canvas como contenido para posicionamiento absoluto
                    var frmCanvas = new Canvas();
                    grp.Content = frmCanvas;

                    control = grp;
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


        public void LoadForm(string vb6Content)
        {
            ClearCanvas();

            var rootModel = ParseVb6Form(vb6Content);
            if (rootModel == null) return;

            // Configurar Tamaño del Formulario
            double fWidth = 600; // Valor default (en pixeles)
            double fHeight = 450;

            if (rootModel.Properties.ContainsKey("ClientWidth")) fWidth = TwipsToPixels(rootModel.Properties["ClientWidth"]);
            else if (rootModel.Properties.ContainsKey("ScaleWidth")) fWidth = TwipsToPixels(rootModel.Properties["ScaleWidth"]);

            if (rootModel.Properties.ContainsKey("ClientHeight")) fHeight = TwipsToPixels(rootModel.Properties["ClientHeight"]);
            else if (rootModel.Properties.ContainsKey("ScaleHeight")) fHeight = TwipsToPixels(rootModel.Properties["ScaleHeight"]);

            if (fWidth > 0 && fHeight > 0) SetFormDimensions(fWidth, fHeight);

            if (rootModel.Properties.ContainsKey("Caption"))
                FormTitle = rootModel.Properties["Caption"];

            // Iniciar renderizado recursivo en el Canvas principal
            RenderChildren(rootModel, DesignSurface);
        }

        private void RenderChildren(VbControlModel model, FrameworkElement container)
        {
            // PASO A: Determinar cuál es el Canvas real donde vamos a agregar los hijos.
            // PictureBox es un Border que tiene un Canvas adentro (Child).
            // Frame es un GroupBox que tiene un Canvas adentro (Content).
            // El formulario principal es un Canvas directo.

            Panel targetPanel = null;

            if (container is Panel p)
            {
                targetPanel = p; // Es un Canvas o Grid directo
            }
            else if (container is Border b && b.Child is Panel childPanel)
            {
                targetPanel = childPanel; // Es un PictureBox, usamos su Canvas interno
            }
            else if (container is GroupBox g && g.Content is Panel contentPanel)
            {
                targetPanel = contentPanel; // Es un Frame, usamos su Canvas interno
            }

            // Si no encontramos un lugar donde poner controles hijos, salimos.
            if (targetPanel == null) return;

            // PASO B: Recorrer y crear hijos
            foreach (var child in model.Children)
            {
                // Filtros (Menús, Timers, etc. que no se dibujan igual)
                if (child.Type.Contains("Menu")) continue;

                // Limpiar nombre del tipo
                string type = child.Type.Contains(".") ? child.Type.Split('.')[1] : child.Type;

                // Fallbacks para controles desconocidos o user controls
                if (child.Type.Contains("ucBtnSkin") || child.Type.Contains("Toolbar")) type = "CommandButton";
                if (child.Type.Contains("ListView")) type = "ListBox";
                if (child.Type.Contains("ImageList")) type = "Timer"; // Usamos Timer como placeholder de control invisible

                // 1. Crear Control
                UIElement element = CreateRetroControl(type);

                if (element != null)
                {
                    var frameworkElement = element as FrameworkElement;

                    // 2. Dimensiones y Posición (Convertir Twips a Pixels)
                    double left = TwipsToPixels(GetPropVal(child, "Left"));
                    double top = TwipsToPixels(GetPropVal(child, "Top"));
                    double w = TwipsToPixels(GetPropVal(child, "Width"));
                    double h = TwipsToPixels(GetPropVal(child, "Height"));

                    if (w > 0) frameworkElement.Width = w;
                    if (h > 0) frameworkElement.Height = h;

                    // 3. Propiedades Básicas
                    if (element is ContentControl cc && child.Properties.ContainsKey("Caption"))
                        cc.Content = child.Properties["Caption"];

                    if (element is TextBox tb && child.Properties.ContainsKey("Text"))
                        tb.Text = child.Properties["Text"];

                    if (child.Properties.ContainsKey("Index"))
                        frameworkElement.Tag = "Array: " + child.Properties["Index"]; // Para saber si es array

                    // 4. Agregar al Panel correcto
                    targetPanel.Children.Add(element);
                    Canvas.SetLeft(element, left);
                    Canvas.SetTop(element, top);

                    // 5. RECURSIVIDAD: Si este hijo tiene sus propios hijos (ej: es un PictureBox con botones)
                    if (child.Children.Count > 0)
                    {
                        // Pasamos este nuevo control como contenedor para la siguiente vuelta
                        RenderChildren(child, frameworkElement);
                    }
                }
            }
        }

        // Helpers
        private double GetPropVal(VbControlModel m, string key)
        {
            if (m.Properties.ContainsKey(key) && double.TryParse(m.Properties[key], out double val))
                return val;
            return 0;
        }

        private double TwipsToPixels(double twips)
        {
            return twips / 15.0; // Conversión aproximada estándar
        }

        private double TwipsToPixels(string twipsStr)
        {
            if (double.TryParse(twipsStr, out double d)) return d / 15.0;
            return 0;
        }






    }


}
