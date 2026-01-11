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
                // =========================================================
                // MENU AVANZADO (Padres e Hijos Dinámicos)
                // =========================================================

                case "Menu":
                    // 1. Crear la factoría del panel para que sea Horizontal
                    var panelFactory = new FrameworkElementFactory(typeof(VirtualizingStackPanel));
                    // Configurar la propiedad Orientation fuera del inicializador
                    panelFactory.SetValue(VirtualizingStackPanel.OrientationProperty, Orientation.Horizontal);

                    // 2. Crear el Template usando la factoría ya configurada
                    var itemsPanelTemplate = new ItemsPanelTemplate(panelFactory);

                    // 3. Crear el Menú
                    var menuBar = new Menu
                    {
                        Height = 20,
                        Background = vbGray,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        SnapsToDevicePixels = true,
                        ItemsPanel = itemsPanelTemplate // Asignamos el template aquí
                    };

                    // --- LÓGICA DE EDICIÓN (Context Menu para la barra) ---
                    var barContextMenu = new ContextMenu();
                    var addTopLevel = new MenuItem { Header = "Agregar Menú Padre (Top Level)" };

                    // Handler para agregar items principales
                    addTopLevel.Click += (s, e) =>
                    {
                        // Nota: Asegúrate de tener el método CreateRetroMenuItem definido en la clase
                        var newItem = CreateRetroMenuItem("Nuevo Menú", fontParams.Family, fontParams.Size);
                        menuBar.Items.Add(newItem);
                    };

                    barContextMenu.Items.Add(addTopLevel);
                    menuBar.ContextMenu = barContextMenu;

                    // --- ITEMS POR DEFECTO ---
                    // Item 1: Archivo
                    var fileMenu = CreateRetroMenuItem("Archivo", fontParams.Family, fontParams.Size);
                    fileMenu.Items.Add(CreateRetroMenuItem("Nuevo", fontParams.Family, fontParams.Size));
                    fileMenu.Items.Add(CreateRetroMenuItem("Abrir...", fontParams.Family, fontParams.Size));
                    fileMenu.Items.Add(new Separator());
                    fileMenu.Items.Add(CreateRetroMenuItem("Salir", fontParams.Family, fontParams.Size));

                    // Item 2: Edición
                    var editMenu = CreateRetroMenuItem("Edición", fontParams.Family, fontParams.Size);
                    editMenu.Items.Add(CreateRetroMenuItem("Copiar", fontParams.Family, fontParams.Size));

                    // Agregar al menú principal
                    menuBar.Items.Add(fileMenu);
                    menuBar.Items.Add(editMenu);
                    menuBar.Items.Add(CreateRetroMenuItem("Ayuda", fontParams.Family, fontParams.Size));

                    element = menuBar;
                    break;


                // =========================================================
                // COLECCIÓN THREED32.OCX (SHERIDAN 3D CONTROLS)
                // =========================================================

                case "SSPanel":
                    // El SSPanel es fundamentalmente un Border con efectos de bisel
                    var ssPanel = new Border
                    {
                        Width = 150,
                        Height = 40,
                        Background = vbGray,
                        // Simulación de bisel 'Inset' (Hundido) clásico de Sheridan
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        SnapsToDevicePixels = true,
                        // Sombra ligera para diferenciarlo del Form
                        Effect = new System.Windows.Media.Effects.DropShadowEffect { ShadowDepth = 1, Color = Colors.White, Direction = -45, BlurRadius = 0, Opacity = 0.5 }
                    };

                    // Lógica de Contenedor (Igual que el Frame)
                    var panelCanvas = new Canvas();
                    panelCanvas.HorizontalAlignment = HorizontalAlignment.Stretch;
                    panelCanvas.VerticalAlignment = VerticalAlignment.Stretch;
                    panelCanvas.Background = Brushes.Transparent; // Necesario para hit-test
                    panelCanvas.MinWidth = 50;
                    panelCanvas.MinHeight = 20;

                    // Texto por defecto centrado (típico de SSPanel)
                    var panelLabel = new TextBlock
                    {
                        Text = "SSPanel1",
                        Foreground = Brushes.Black,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false // Para no bloquear clicks al canvas
                    };

                    // Usamos un Grid para superponer el Canvas (drop area) y el Texto
                    var panelGrid = new Grid();
                    panelGrid.Children.Add(panelLabel); // Fondo: Texto
                    panelGrid.Children.Add(panelCanvas); // Frente: Zona de drop

                    ssPanel.Child = panelGrid;
                    element = ssPanel;
                    break;

                case "SSCommand":
                    // El botón de Sheridan solía ser más "cuadrado" o permitir iconos+texto
                    var ssCmd = new Button
                    {
                        Content = "SSCommand1",
                        Width = 100,
                        Height = 35, // Solían ser un poco más altos
                        Background = vbGray,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        FontWeight = FontWeights.Bold // Solían verse más "pesados"
                    };
                    // Hack visual para diferenciarlo del CommandButton normal: Borde más grueso
                    ssCmd.BorderThickness = new Thickness(2);
                    element = ssCmd;
                    break;

                case "SSCheck":
                    // Checkbox con estilo 3D (a menudo se veía como un botón toggle)
                    var ssCheck = new CheckBox
                    {
                        Content = "SSCheck1",
                        Width = 121,
                        Height = 25,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1) // Borde alrededor del control entero
                    };
                    // Padding extra para simular el estilo 'Panel' del check
                    ssCheck.Padding = new Thickness(5, 0, 0, 0);
                    element = ssCheck;
                    break;

                case "SSOption":
                    var ssOpt = new RadioButton
                    {
                        Content = "SSOption1",
                        Width = 121,
                        Height = 25,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1)
                    };
                    ssOpt.Padding = new Thickness(5, 0, 0, 0);
                    element = ssOpt;
                    break;

                case "SSFrame":
                    // El Frame de Sheridan tenía bordes más gruesos y configurables
                    var ssGrp = new GroupBox
                    {
                        Header = "SSFrame1",
                        Width = 180,
                        Height = 120,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        Background = vbGray,
                        BorderBrush = Brushes.Black, // Borde negro más marcado
                        BorderThickness = new Thickness(1)
                    };

                    // Lógica de contenedor crítica
                    var ssFrameCanvas = new Canvas
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Background = Brushes.Transparent,
                        MinHeight = 50,
                        MinWidth = 50
                    };
                    ssGrp.Content = ssFrameCanvas;
                    element = ssGrp;
                    break;

                case "SSRibbon":
                    // Botones de Toolbar (Picture + Label, o solo Picture)
                    // Usaremos un ToggleButton para simular el comportamiento de "Grupo"
                    var ssRib = new System.Windows.Controls.Primitives.ToggleButton
                    {
                        Content = "Ribbon",
                        Width = 40,
                        Height = 40,
                        Background = vbGray,
                        FontFamily = fontParams.Family,
                        FontSize = 9 // Fuente pequeña típica de toolbars
                    };
                    element = ssRib;
                    break;
                // =========================================================
                // GRID32.OCX (Microsoft Grid Control)
                // =========================================================

                case "Grid":
                    // El Grid32 clásico tiene un borde hundido
                    var gridBorder = new Border
                    {
                        Width = 200,
                        Height = 150,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        SnapsToDevicePixels = true,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect { ShadowDepth = 0, BlurRadius = 0 } // Borde simple
                    };

                    // Estructura interna: Cabeceras y celdas
                    var mainGrid = new Grid();
                    mainGrid.Background = Brushes.White; // El área de datos es blanca

                    // Definimos filas simuladas
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) }); // Fixed Row (Header)
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) }); // Row 1
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) }); // Row 2
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Resto

                    // Definimos columnas simuladas
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); // Fixed Col
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) }); // Col A
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) }); // Col B
                    mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Resto

                    // --- 1. CABECERA SUPERIOR (Fixed Row) ---
                    var topHeader = new Border
                    {
                        Background = vbGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0, 0, 0, 1)
                    };
                    Grid.SetRow(topHeader, 0);
                    Grid.SetColumnSpan(topHeader, 4);
                    mainGrid.Children.Add(topHeader);

                    // --- 2. CABECERA LATERAL (Fixed Col) ---
                    var leftHeader = new Border
                    {
                        Background = vbGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0, 0, 1, 0)
                    };
                    Grid.SetRow(leftHeader, 0);
                    Grid.SetRowSpan(leftHeader, 4);
                    mainGrid.Children.Add(leftHeader);

                    // --- 3. INTERSECCIÓN (Esquina superior izquierda) ---
                    // Un pequeño bloque gris levantado
                    var cornerBtn = new Border
                    {
                        Background = vbGray,
                        BorderBrush = Brushes.White, // Simula efecto 3D simple
                        BorderThickness = new Thickness(1, 1, 0, 0)
                    };
                    Grid.SetRow(cornerBtn, 0);
                    Grid.SetColumn(cornerBtn, 0);
                    mainGrid.Children.Add(cornerBtn);

                    // --- 4. LÍNEAS DE REJILLA (GridLines visuales) ---
                    // Dibujamos algunas líneas verticales grises para simular columnas
                    for (int i = 1; i <= 2; i++)
                    {
                        var vLine = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 1, 0) };
                        Grid.SetRow(vLine, 1);
                        Grid.SetRowSpan(vLine, 3);
                        Grid.SetColumn(vLine, i);
                        mainGrid.Children.Add(vLine);
                    }

                    // Dibujamos algunas líneas horizontales
                    for (int i = 1; i <= 2; i++)
                    {
                        var hLine = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0, 0, 0, 1) };
                        Grid.SetRow(hLine, i);
                        Grid.SetColumn(hLine, 1);
                        Grid.SetColumnSpan(hLine, 3);
                        mainGrid.Children.Add(hLine);
                    }

                    gridBorder.Child = mainGrid;
                    element = gridBorder;
                    break;

                // =========================================================
                // GRAPH32.OCX (Microsoft Chart Control)
                // =========================================================

                case "Graph":
                    // Contenedor principal
                    var graphBorder = new Border
                    {
                        Width = 200,
                        Height = 150,
                        Background = Brushes.White,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect { ShadowDepth = 2, Color = Colors.Black, Opacity = 0.3, BlurRadius = 4 }
                    };

                    var gCanvas = new Canvas();

                    // 1. Título del Gráfico (Típico default: "Graph Title")
                    var title = new TextBlock
                    {
                        Text = "Graph Title",
                        FontWeight = FontWeights.Bold,
                        FontSize = 10,
                        Foreground = Brushes.Black
                    };
                    Canvas.SetTop(title, 5);
                    Canvas.SetLeft(title, 70); // Centrado a ojo
                    gCanvas.Children.Add(title);

                    // 2. Ejes (Líneas)
                    // Eje Y
                    var yAxis = new Line { X1 = 20, Y1 = 20, X2 = 20, Y2 = 130, Stroke = Brushes.Black, StrokeThickness = 1 };
                    gCanvas.Children.Add(yAxis);
                    // Eje X
                    var xAxis = new Line { X1 = 20, Y1 = 130, X2 = 190, Y2 = 130, Stroke = Brushes.Black, StrokeThickness = 1 };
                    gCanvas.Children.Add(xAxis);

                    // 3. Barras (Datos Simulados)
                    // Colores clásicos de Graph32
                    Brush[] barColors = { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Magenta, Brushes.Yellow };
                    double[] barHeights = { 60, 90, 40, 80, 50 }; // Alturas simuladas

                    double startX = 30;
                    double barWidth = 20;
                    double gap = 10;
                    double groundY = 130;

                    for (int i = 0; i < 5; i++)
                    {
                        var bar = new System.Windows.Shapes.Rectangle
                        {
                            Width = barWidth,
                            Height = barHeights[i],
                            Fill = barColors[i],
                            Stroke = Brushes.Black, // Borde negro fino para que resalte
                            StrokeThickness = 0.5
                        };

                        // Posicionar (Recordar que en Canvas Y crece hacia abajo, así que Top = Suelo - Altura)
                        Canvas.SetLeft(bar, startX + (i * (barWidth + gap)));
                        Canvas.SetTop(bar, groundY - barHeights[i]);

                        gCanvas.Children.Add(bar);
                    }

                    // 4. Leyenda pequeña (Opcional, pero le da el toque)
                    var legendText = new TextBlock { Text = "Data", FontSize = 8, Foreground = Brushes.Gray };
                    Canvas.SetLeft(legendText, 160);
                    Canvas.SetTop(legendText, 10);
                    gCanvas.Children.Add(legendText);

                    graphBorder.Child = gCanvas;
                    element = graphBorder;
                    break;
                case "MaskEdBox":
                    // Msmask32.ocx - Simulación visual
                    var maskBox = new TextBox
                    {
                        Text = "__/__/____", // Máscara típica de fecha por defecto
                        Width = 100,
                        Height = 25,
                        FontFamily = new FontFamily("Courier New"), // Monoespaciado para que cuadre la máscara
                        FontSize = fontParams.Size,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    element = maskBox;
                    break;

                case "CommonDialog":
                    // comdlg32.ocx - Control invisible (Icono en diseño)
                    var cmnDlg = new Border
                    {
                        Width = 32,
                        Height = 32,
                        Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1)
                    };
                    // Icono simulado (varias ventanitas)
                    var cmnCanvas = new Canvas();
                    cmnCanvas.Children.Add(new System.Windows.Shapes.Rectangle { Width = 20, Height = 14, Stroke = Brushes.Black, StrokeThickness = 1, Fill = Brushes.White, RadiusX = 1, RadiusY = 1 }); // Ventana ppal
                    Canvas.SetLeft(cmnCanvas.Children[0], 2); Canvas.SetTop(cmnCanvas.Children[0], 2);

                    cmnDlg.Child = cmnCanvas;
                    element = cmnDlg;
                    break;

                case "CrystalReport":
                    // Crystl32.OCX - Control invisible (Icono de diamante en diseño)
                    var cryRep = new Border
                    {
                        Width = 32,
                        Height = 32,
                        Background = Brushes.LightGray,
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1)
                    };

                    var cryGrid = new Grid();
                    // Rombo (Diamante)
                    var diamond = new System.Windows.Shapes.Path
                    {
                        Data = Geometry.Parse("M 15,2 L 28,15 L 15,28 L 2,15 Z"),
                        Fill = Brushes.Blue, // El icono clásico era azulado/cian
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Stretch = Stretch.Uniform,
                        Margin = new Thickness(4)
                    };
                    cryGrid.Children.Add(diamond);

                    cryRep.Child = cryGrid;
                    element = cryRep;
                    break;

                // =========================================================
                // MAP60.OCX (COBIS Map Control)
                // =========================================================

                case "Map":
                    var mapBorder = new Border
                    {
                        Width = 100,
                        Height = 100,
                        Background = new SolidColorBrush(Color.FromRgb(220, 240, 255)), // Azul claro tipo agua/mapa
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1)
                    };

                    var mapCanvas = new Canvas();

                    // Dibujar algunas "calles" simuladas
                    mapCanvas.Children.Add(new System.Windows.Shapes.Path
                    {
                        Data = Geometry.Parse("M 0,30 L 100,30 M 30,0 L 30,100 M 0,70 L 100,70 M 70,0 L 70,100"),
                        Stroke = Brushes.White,
                        StrokeThickness = 2
                    });

                    // Icono de "Pin" central
                    var pinPath = new System.Windows.Shapes.Path
                    {
                        Data = Geometry.Parse("M 10,0 C 4.5,0 0,4.5 0,10 C 0,17 10,28 10,28 C 10,28 20,17 20,10 C 20,4.5 15.5,0 10,0 Z M 10,7 A 3,3 0 1 1 10,13 A 3,3 0 1 1 10,7 Z"),
                        Fill = Brushes.Red,
                        Stroke = Brushes.DarkRed,
                        StrokeThickness = 1
                    };
                    // Centrar el pin
                    Canvas.SetLeft(pinPath, 40);
                    Canvas.SetTop(pinPath, 35);

                    mapCanvas.Children.Add(pinPath);

                    // Etiqueta
                    var mapLabel = new TextBlock
                    {
                        Text = "Map32",
                        FontSize = 9,
                        Foreground = Brushes.Gray,
                        FontWeight = FontWeights.Bold
                    };
                    Canvas.SetLeft(mapLabel, 2);
                    Canvas.SetTop(mapLabel, 2);
                    mapCanvas.Children.Add(mapLabel);

                    mapBorder.Child = mapCanvas;
                    element = mapBorder;
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


        // Método Helper para crear MenuItems con estilo Retro y capacidad de agregar hijos
        private static MenuItem CreateRetroMenuItem(string header, FontFamily family, double size)
        {
            var item = new MenuItem
            {
                Header = header,
                FontFamily = family,
                FontSize = size,
                Foreground = Brushes.Black,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8")),
                BorderThickness = new Thickness(0)
            };

            var itemCtxMenu = new ContextMenu();

            // Opción: Agregar Hijo
            var addChild = new MenuItem { Header = "Agregar Sub-Item (Hijo)" };
            addChild.Click += (s, e) =>
            {
                var newChild = CreateRetroMenuItem("Nuevo Sub-Item", family, size);
                item.Items.Add(newChild);
                item.IsSubmenuOpen = true;
            };

            // Opción: Cambiar Texto (Versión simple sin InputBox)
            var rename = new MenuItem { Header = "Cambiar Texto..." };
            rename.Click += (s, e) =>
            {
                // Alternar texto simple para pruebas
                item.Header = item.Header.ToString() == "Nuevo Menú" ? "Opción X" : "Editado";
            };

            // Opción: Eliminar
            var delete = new MenuItem { Header = "Eliminar este Item" };
            delete.Click += (s, e) =>
            {
                if (item.Parent is ItemsControl parentIC)
                {
                    parentIC.Items.Remove(item);
                }
            };

            itemCtxMenu.Items.Add(addChild);
            itemCtxMenu.Items.Add(rename);
            itemCtxMenu.Items.Add(new Separator());
            itemCtxMenu.Items.Add(delete);

            item.ContextMenu = itemCtxMenu;

            return item;
        }
    }
}
