using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace VB6VisualMockupDesigner.Controls
{
    /// <summary>
    /// Factory class for creating VB6-style controls with retro styling.
    /// </summary>
    public static class RetroControlFactory
    {
        // =========================================================
        // HELPER: Crea un Canvas interno para contenedores
        // =========================================================
        private static Canvas CreateChildCanvas()
        {
            return new Canvas
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent, // Crucial para detectar Drag & Drop
                ClipToBounds = true,
                MinWidth = 10,
                MinHeight = 10
            };
        }

        // =========================================================
        // MÉTODO PRINCIPAL: FABRICA DE CONTROLES
        // =========================================================
        
        /// <summary>
        /// Creates a VB6-style control of the specified type.
        /// </summary>
        /// <param name="type">The type of control to create (e.g., "Label", "TextBox", "CommandButton").</param>
        /// <returns>A UIElement representing the created control, or null if the type is invalid.</returns>
        public static UIElement Create(string type)
        {
            FrameworkElement element = null;

            // Recursos y Estilos Comunes
            var fontParams = new { Family = new FontFamily("Microsoft Sans Serif"), Size = 11.0 };
            var vbGray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8"));
            var vbBlack = Brushes.Black;

            switch (type)
            {
                case "Pointer": return null;

                // -----------------------------------------------------
                // CONTROLES ESTÁNDAR DE VB6
                // -----------------------------------------------------

                case "PictureBox":
                    var picBorder = new Border
                    {
                        Width = 100,
                        Height = 100,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(2),
                        ClipToBounds = true
                    };
                    picBorder.Child = CreateChildCanvas();
                    element = picBorder;
                    break;

                case "Label":
                    // LÓGICA UNIFICADA:
                    // Se crea transparente y sin borde por defecto.
                    // PropertyManager cambiará BackColor/Border si es necesario.
                    element = new Label
                    {
                        Content = "Label1",
                        Width = 121,
                        Height = 25,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(2, 0, 0, 0), // Padding ligero por defecto
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    break;

                case "TextBox":
                    element = new TextBox
                    {
                        Text = "Text1",
                        Width = 121,
                        Height = 25,
                        Style = RetroStyles.GetVB6TextBoxStyle()
                    };
                    break;

                case "Frame":
                    var grp = new GroupBox
                    {
                        Header = "Frame1",
                        Width = 200,
                        Height = 150,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray
                    };
                    grp.Content = CreateChildCanvas();
                    element = grp;
                    break;

                case "CommandButton":
                    element = new Button
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
                    element = new CheckBox
                    {
                        Content = "Check1",
                        Width = 121,
                        Height = 25,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    break;

                case "OptionButton":
                    element = new RadioButton
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
                    element = new ComboBox
                    {
                        Width = 121,
                        Height = 21,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        IsEditable = true,
                        Text = "Combo1"
                    };
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
                    list.Items.Add("List1");
                    element = list;
                    break;

                case "HScrollBar":
                    element = new ScrollBar
                    {
                        Orientation = Orientation.Horizontal,
                        Width = 100,
                        Height = 17,
                        Value = 50,
                        Maximum = 100,
                        Style = RetroStyles.GetVB6ScrollBarStyle(Orientation.Horizontal)
                    };
                    break;

                case "VScrollBar":
                    element = new ScrollBar
                    {
                        Orientation = Orientation.Vertical,
                        Width = 17,
                        Height = 100,
                        Value = 50,
                        Maximum = 100,
                        Style = RetroStyles.GetVB6ScrollBarStyle(Orientation.Vertical)
                    };
                    break;

                case "Timer":
                    element = new Border
                    {
                        Width = 34,
                        Height = 34,
                        Background = vbGray,
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Child = new TextBlock { Text = "Timer", FontSize = 8, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center }
                    };
                    break;

                case "DriveListBox":
                    element = new ComboBox
                    {
                        Width = 121,
                        Height = 21,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        Text = @"c: [OS]"
                    };
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
                    element = dirList;
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
                    element = fileList;
                    break;

                case "Shape":
                    element = new System.Windows.Shapes.Rectangle
                    {
                        Width = 50,
                        Height = 50,
                        Stroke = vbBlack,
                        StrokeThickness = 1,
                        Fill = Brushes.Transparent
                    };
                    break;

                case "Line":
                    element = new System.Windows.Shapes.Rectangle
                    {
                        Width = 100,
                        Height = 2,
                        Fill = vbBlack
                    };
                    break;

                case "Image":
                    var imgGrid = new Grid { Width = 100, Height = 100, Background = Brushes.Transparent };
                    imgGrid.Children.Add(new System.Windows.Shapes.Rectangle
                    {
                        Stroke = Brushes.Gray,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection() { 4, 2 },
                        Fill = Brushes.Transparent,
                        IsHitTestVisible = false
                    });
                    imgGrid.Children.Add(new TextBlock
                    {
                        Text = "Image",
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false
                    });
                    element = imgGrid;
                    break;

                case "Data":
                    element = new Button { Content = "Data Control", Width = 150, Height = 25, Background = vbGray };
                    break;

                case "OLE":
                    element = new Border
                    {
                        Width = 75,
                        Height = 75,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        Background = Brushes.LightGray,
                        Child = new TextBlock { Text = "OLE", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
                    };
                    break;

                // -----------------------------------------------------
                // MENÚS
                // -----------------------------------------------------
                case "Menu":
                    var menuBar = new Menu
                    {
                        Height = 22,
                        Style = RetroStyles.GetVB6MenuStyle()
                    };

                    // Context Menu para agregar items
                    var barContextMenu = new ContextMenu();
                    var addTopLevel = new MenuItem { Header = "Agregar Menú Padre (Top Level)" };
                    addTopLevel.Click += (s, e) =>
                    {
                        var dialog = new SimpleInputDialog("Nombre del Menú Principal:", "Nuevo Menú");
                        if (dialog.ShowDialog() == true)
                        {
                            var newItem = CreateRetroMenuItem(dialog.Answer, fontParams.Family, fontParams.Size);
                            menuBar.Items.Add(newItem);
                        }
                    };
                    barContextMenu.Items.Add(addTopLevel);
                    menuBar.ContextMenu = barContextMenu;

                    // Items por defecto
                    var fileMenu = CreateRetroMenuItem("_Archivo", fontParams.Family, fontParams.Size);
                    fileMenu.Items.Add(CreateRetroMenuItem("Nuevo", fontParams.Family, fontParams.Size));
                    fileMenu.Items.Add(CreateRetroMenuItem("Abrir...", fontParams.Family, fontParams.Size));
                    fileMenu.Items.Add(new Separator());
                    fileMenu.Items.Add(CreateRetroMenuItem("Salir", fontParams.Family, fontParams.Size));

                    menuBar.Items.Add(fileMenu);
                    element = menuBar;
                    break;

                // -----------------------------------------------------
                // THREED32.OCX (SHERIDAN)
                // -----------------------------------------------------
                case "SSPanel":
                    var ssPanel = new Border
                    {
                        Width = 150,
                        Height = 40,
                        Background = vbGray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        SnapsToDevicePixels = true,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect { ShadowDepth = 1, Color = Colors.White, Direction = -45, BlurRadius = 0, Opacity = 0.5 }
                    };
                    var panelGrid = new Grid();
                    panelGrid.Children.Add(new TextBlock
                    {
                        Text = "SSPanel1",
                        Foreground = Brushes.Black,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false
                    });
                    panelGrid.Children.Add(CreateChildCanvas()); // Capa de hijos
                    ssPanel.Child = panelGrid;
                    element = ssPanel;
                    break;

                case "SSCommand":
                    element = new Button
                    {
                        Content = "SSCommand1",
                        Width = 100,
                        Height = 35,
                        Style = RetroStyles.GetCustomThreed32Style()
                    };
                    break;

                case "SSCheck":
                    var ssCheck = new CheckBox
                    {
                        Content = "SSCheck1",
                        Width = 121,
                        Height = 25,
                        Style = RetroStyles.GetCustomThreed32CheckStyle(),
                        Padding = new Thickness(5, 0, 0, 0)
                    };
                    element = ssCheck;
                    break;

                case "SSOption":
                    element = new RadioButton
                    {
                        Content = "SSOption1",
                        Width = 121,
                        Height = 25,
                        Style = RetroStyles.GetCustomThreed32OptionStyle()
                    };
                    break;

                case "SSFrame":
                    var ssGrp = new GroupBox
                    {
                        Header = "SSFrame1",
                        Width = 180,
                        Height = 120,
                        FontFamily = fontParams.Family,
                        FontSize = fontParams.Size,
                        Background = vbGray,
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1)
                    };
                    ssGrp.Content = CreateChildCanvas();
                    element = ssGrp;
                    break;

                case "SSRibbon":
                    element = new ToggleButton
                    {
                        Content = "Ribbon",
                        Width = 40,
                        Height = 40,
                        Background = vbGray,
                        FontFamily = fontParams.Family,
                        FontSize = 9
                    };
                    break;
                case "SSTab":
                    var tabControl = new TabControl
                    {
                        Width = 240,
                        Height = 180,
                        Style = RetroStyles.GetSSTabStyle()
                    };

                    // Crear 3 pestañas por defecto simulando el comportamiento de VB6
                    for (int i = 0; i < 3; i++)
                    {
                        var tab = new TabItem
                        {
                            Header = $"Tab {i}"
                        };

                        // ¡CRUCIAL! Cada pestaña tiene su propio Canvas para recibir controles
                        tab.Content = CreateChildCanvas();

                        tabControl.Items.Add(tab);
                    }

                    // Seleccionar la primera por defecto
                    tabControl.SelectedIndex = 0;

                    element = tabControl;
                    break;

                // -----------------------------------------------------
                // GRID32.OCX
                // -----------------------------------------------------
                case "Grid":
                    var gridBorder = new Border
                    {
                        Width = 300,
                        Height = 150,
                        Background = Brushes.Gray,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        SnapsToDevicePixels = true
                    };

                    var layoutGrid = new Grid();
                    layoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    layoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(17) });
                    layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(17) });

                    // Simulación visual del grid
                    var tableArea = new Grid();
                    tableArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) }); // Header
                    tableArea.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Body

                    var headerCanvas = new Canvas { Background = vbGray, ClipToBounds = true };
                    Grid.SetRow(headerCanvas, 0);
                    tableArea.Children.Add(headerCanvas);

                    var bodyCanvas = new Canvas { Background = Brushes.White, ClipToBounds = true };
                    Grid.SetRow(bodyCanvas, 1);
                    tableArea.Children.Add(bodyCanvas);

                    Grid.SetColumn(tableArea, 0); Grid.SetRow(tableArea, 0);
                    layoutGrid.Children.Add(tableArea);

                    // Scrollbars simulados
                    var vScroll = new ScrollBar { Orientation = Orientation.Vertical, Width = 17, Value = 0, Maximum = 100 };
                    Grid.SetColumn(vScroll, 1); Grid.SetRow(vScroll, 0);
                    layoutGrid.Children.Add(vScroll);

                    var hScroll = new ScrollBar { Orientation = Orientation.Horizontal, Height = 17, Value = 0, Maximum = 100 };
                    Grid.SetColumn(hScroll, 0); Grid.SetRow(hScroll, 1);
                    layoutGrid.Children.Add(hScroll);

                    var corner = new Border { Background = vbGray };
                    Grid.SetColumn(corner, 1); Grid.SetRow(corner, 1);
                    layoutGrid.Children.Add(corner);

                    gridBorder.Child = layoutGrid;

                    // Dibujado simple de líneas al redimensionar
                    gridBorder.SizeChanged += (s, e) =>
                    {
                        headerCanvas.Children.Clear();
                        bodyCanvas.Children.Clear();
                        double w = tableArea.ActualWidth;
                        double bodyH = bodyCanvas.ActualHeight;
                        if (w <= 0 || bodyH <= 0) return;

                        double cellW = 70; double cellH = 20;
                        int col = 0;
                        for (double x = 0; x < w; x += cellW)
                        {
                            col++;
                            // Header
                            var hCell = new Border { Width = cellW, Height = cellH, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(0, 0, 1, 1), Background = (col == 1) ? Brushes.DarkGray : vbGray };
                            if (col > 1) hCell.Child = new TextBlock { Text = $"Col {col - 1}", FontSize = fontParams.Size, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                            Canvas.SetLeft(hCell, x); headerCanvas.Children.Add(hCell);

                            // VLine
                            bodyCanvas.Children.Add(new Line { X1 = x + cellW, Y1 = 0, X2 = x + cellW, Y2 = bodyH, Stroke = Brushes.LightGray });
                        }
                        for (double y = 0; y < bodyH; y += cellH)
                        {
                            bodyCanvas.Children.Add(new Line { X1 = 0, Y1 = y, X2 = w, Y2 = y, Stroke = Brushes.LightGray });
                        }
                    };

                    element = gridBorder;
                    break;

                // -----------------------------------------------------
                // GRAPH32.OCX
                // -----------------------------------------------------
                case "Graph":
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
                    gCanvas.Children.Add(new TextBlock { Text = "Graph Title", FontWeight = FontWeights.Bold, FontSize = 10 });
                    Canvas.SetLeft(gCanvas.Children[0], 70); Canvas.SetTop(gCanvas.Children[0], 5);

                    // Ejes y Barras simples
                    gCanvas.Children.Add(new Line { X1 = 20, Y1 = 20, X2 = 20, Y2 = 130, Stroke = Brushes.Black });
                    gCanvas.Children.Add(new Line { X1 = 20, Y1 = 130, X2 = 190, Y2 = 130, Stroke = Brushes.Black });

                    Brush[] barColors = { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Magenta, Brushes.Yellow };
                    double[] barH = { 60, 90, 40, 80, 50 };
                    for (int i = 0; i < 5; i++)
                    {
                        var bar = new System.Windows.Shapes.Rectangle { Width = 20, Height = barH[i], Fill = barColors[i], Stroke = Brushes.Black, StrokeThickness = 0.5 };
                        Canvas.SetLeft(bar, 30 + (i * 30));
                        Canvas.SetTop(bar, 130 - barH[i]);
                        gCanvas.Children.Add(bar);
                    }
                    graphBorder.Child = gCanvas;
                    element = graphBorder;
                    break;

                // -----------------------------------------------------
                // OTROS CONTROLES OCX
                // -----------------------------------------------------
                case "MaskEdBox":
                    element = new TextBox
                    {
                        Text = "__/__/____",
                        Width = 100,
                        Height = 25,
                        FontFamily = new FontFamily("Courier New"),
                        FontSize = fontParams.Size,
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(1),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };
                    break;

                case "CommonDialog":
                    var cmnDlg = new Border { Width = 32, Height = 32, Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
                    var cmnC = new Canvas();
                    cmnC.Children.Add(new System.Windows.Shapes.Rectangle { Width = 20, Height = 14, Stroke = Brushes.Black, StrokeThickness = 1, Fill = Brushes.White });
                    Canvas.SetLeft(cmnC.Children[0], 6); Canvas.SetTop(cmnC.Children[0], 8);
                    cmnDlg.Child = cmnC;
                    element = cmnDlg;
                    break;

                case "CrystalReport":
                    var cryRep = new Border { Width = 32, Height = 32, Background = Brushes.LightGray, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
                    var cryPath = new System.Windows.Shapes.Path { Data = Geometry.Parse("M 15,2 L 28,15 L 15,28 L 2,15 Z"), Fill = Brushes.Blue, Stroke = Brushes.Black, Stretch = Stretch.Uniform, Margin = new Thickness(4) };
                    cryRep.Child = cryPath;
                    element = cryRep;
                    break;

                case "Map":
                    var mapBorder = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Color.FromRgb(220, 240, 255)), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
                    var mapC = new Canvas();
                    mapC.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 0,30 L 100,30 M 30,0 L 30,100"), Stroke = Brushes.White, StrokeThickness = 2 });
                    var pin = new System.Windows.Shapes.Path { Data = Geometry.Parse("M 10,0 C 4.5,0 0,4.5 0,10 C 0,17 10,28 10,28 C 10,28 20,17 20,10 C 20,4.5 15.5,0 10,0 Z"), Fill = Brushes.Red, Stroke = Brushes.DarkRed };
                    Canvas.SetLeft(pin, 40); Canvas.SetTop(pin, 35);
                    mapC.Children.Add(pin);
                    mapBorder.Child = mapC;
                    element = mapBorder;
                    break;

                default:
                    // Placeholder genérico para controles desconocidos
                    var ph = new Grid { Width = 50, Height = 50, Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)) };
                    ph.Children.Add(new System.Windows.Shapes.Rectangle { Stroke = Brushes.Gray, StrokeThickness = 1, StrokeDashArray = new DoubleCollection() { 4, 2 } });
                    ph.Children.Add(new TextBlock { Text = $"[{type}]", Foreground = Brushes.DarkGray, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
                    element = ph;
                    break;
            }

            if (element != null) element.Tag = type;
            return element;
        }

        // =========================================================
        // HELPER PARA MENÚS
        // =========================================================
        private static MenuItem CreateRetroMenuItem(string header, FontFamily family, double size)
        {
            var item = new MenuItem
            {
                Header = header,
                FontFamily = family,
                FontSize = size,
                Foreground = Brushes.Black,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8"))
            };

            var itemCtxMenu = new ContextMenu();
            var addChild = new MenuItem { Header = "Agregar Sub-Item" };
            addChild.Click += (s, e) => { item.Items.Add(CreateRetroMenuItem("Nuevo Item", family, size)); item.IsSubmenuOpen = true; };
            var rename = new MenuItem { Header = "Renombrar..." };
            rename.Click += (s, e) =>
            {
                var dlg = new SimpleInputDialog("Nuevo nombre:", item.Header.ToString());
                if (dlg.ShowDialog() == true) item.Header = dlg.Answer;
            };
            var delete = new MenuItem { Header = "Eliminar" };
            delete.Click += (s, e) => { if (item.Parent is ItemsControl p) p.Items.Remove(item); };

            itemCtxMenu.Items.Add(addChild);
            itemCtxMenu.Items.Add(rename);
            itemCtxMenu.Items.Add(new Separator());
            itemCtxMenu.Items.Add(delete);
            item.ContextMenu = itemCtxMenu;

            return item;
        }
    }

    // =========================================================
    // DIÁLOGO SIMPLE PARA ENTRADA DE TEXTO
    // =========================================================
    public class SimpleInputDialog : Window
    {
        private TextBox _txtInput;
        public string Answer => _txtInput.Text;

        public SimpleInputDialog(string question, string defaultAnswer = "")
        {
            Width = 300; Height = 140; Title = "Entrada";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.ToolWindow;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8"));

            var stack = new StackPanel { Margin = new Thickness(10) };
            stack.Children.Add(new TextBlock { Text = question, Margin = new Thickness(0, 0, 0, 5) });
            _txtInput = new TextBox { Text = defaultAnswer };
            _txtInput.SelectAll();
            stack.Children.Add(_txtInput);

            var btnOk = new Button { Content = "Aceptar", Width = 70, Height = 25, Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Right, IsDefault = true };
            btnOk.Click += (s, e) => { DialogResult = true; };
            stack.Children.Add(btnOk);

            Content = stack;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            _txtInput.Focus();
        }
    }
}