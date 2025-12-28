using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;




namespace VB6VisualMockupDesigner
{

    /// <summary>
    /// Interaction logic for MainWindowModern.xaml
    /// </summary>
    public partial class MainWindowModern : Window
    {
        private ToolboxView _toolboxView; // Instancia única para no recrearla siempre
        private ProjectExplorerView _explorerView; // Nueva referencia

        public MainWindowModern()
        {
            InitializeComponent();

            // Inicializamos el toolbox
            _toolboxView = new ToolboxView();
            _toolboxView.OnControlSelected += Toolbox_ControlSelected;

            _explorerView = new ProjectExplorerView();
            _explorerView.OnFileOpened += Explorer_OnFileOpened;


            // ESTADO INICIAL: Deshabilitado porque no hay proyecto abierto
            _toolboxView.EnableTools(false);

            MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;

            this.StateChanged += MainWindowModern_StateChanged;
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Verificamos que el origen sea el TabControl y no un control hijo
            if (e.Source is TabControl && MainTabControl.SelectedItem is TabItem selectedTab)
            {
                // Obtenemos el nombre del archivo del Header de la pestaña
                string fileName = selectedTab.Header.ToString();

                // Le decimos al explorador que destaque ese archivo
                _explorerView.SelectFile(fileName);
            }
        }

        public void LoadProject(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return;

            // Detectar si es un proyecto (.vbp) o un formulario suelto (.frm)
            string ext = System.IO.Path.GetExtension(fullPath).ToLower();

            if (ext == ".vbp")
            {
                // CASO 1: Cargar Proyecto Completo
                _explorerView.LoadProjectStructure(fullPath);

                // Opcional: Expandir el explorador automáticamente
                OpenSideBar("Explorer");
            }
            else
            {
                // CASO 2: Archivo suelto (.frm)
                // Lo abrimos directo en pestaña
                string fileName = System.IO.Path.GetFileName(fullPath);
                OpenFileTab(fullPath);

                // Y lo agregamos al explorador como un archivo único (modo simple)
                // Para esto podrías crear un método "AddSingleFile" en ExplorerView si quisieras
                // O simplemente dejar que el explorador se quede vacío si no hay proyecto.
            }

            // Registrar en recientes
            RecentFilesManager.AddToRecents(fullPath);
        }

        // ============================================
        // GESTIÓN DE PESTAÑAS
        // ============================================

        private void Explorer_OnFileOpened(object sender, string fileName)
        {
            OpenFileTab(fileName);
        }

        private void OpenFileTab(string title)
        {
            // 1. Verificar si ya está abierta para seleccionarla en vez de duplicar
            foreach (TabItem tab in MainTabControl.Items)
            {
                if (tab.Header.ToString() == title)
                {
                    MainTabControl.SelectedItem = tab;
                    return;
                }
            }

            // 2. Crear nueva pestaña
            var newTab = new TabItem
            {
                Header = title
            };

            var designer = new DesignerCanvas();

            // === NUEVO: CARGAR EL CONTENIDO DEL ARCHIVO ===

            // 1. Buscamos la ruta completa. 
            // Como OpenFileTab a veces solo recibe el nombre (desde el Tab), 
            // necesitamos asegurarnos de tener la ruta completa.
            // TRUCO: Modifica OpenFileTab para recibir la ruta completa, 
            // o búscalo en tu lista de recientes/explorador.

            // Asumiremos que 'title' es la ruta completa o que tienes acceso a ella.
            // SI NO TIENES LA RUTA COMPLETA AQUÍ, DEBES PASARLA.
            // Vamos a asumir que cambias la firma del método o pasas el path.

            string fullPath = title; // Asumiendo que ahora pasas el path completo

            // Si solo pasaste el nombre, el parser fallará gracefully (File.Exists check).
            // Lo ideal es cambiar la llamada OpenFileTab(fileName) a OpenFileTab(fullPath) en todo el código.

            FrmParser.Parse(fullPath, designer);

            // Ajustar el título de la pestaña para que solo muestre el nombre del archivo
            newTab.Header = System.IO.Path.GetFileName(fullPath);

            // === FIN NUEVO ===

            designer.ControlSelected += (s, control) =>
            {
                PropertiesPanel.InspectObject(control);
            };
            // =========================================================

            newTab.Content = designer;

            // Configurar el título interno de la ventana VB6
            designer.FormTitle = title;

            // Asignamos el designer como contenido de la pestaña
            newTab.Content = designer;

            // 4. Agregar y seleccionar
            MainTabControl.Items.Add(newTab);
            MainTabControl.SelectedItem = newTab;

            // 5. Configurar botón de cerrar (Buscamos el botón dentro del template una vez cargado)
            // NOTA: Una forma más limpia en WPF puro es usar MVVM, pero aquí lo haremos con eventos directos.
            // Necesitamos esperar a que se aplique el template o usar un evento en el estilo.
            // TRUCO: Hemos definido el Click en el XAML, pero necesitamos capturarlo globalmente o en el estilo.
            // Vamos a asignar el evento 'Loaded' al TabItem para buscar su botón.
            newTab.Loaded += NewTab_Loaded;

            UpdateTabVisibility();

            // Habilitar Toolbox
            _toolboxView.EnableTools(true);
        }

        private void NewTab_Loaded(object sender, RoutedEventArgs e)
        {
            var tab = sender as TabItem;
            // Buscar el botón "CloseBtn" dentro del Template del TabItem
            if (tab.Template.FindName("CloseBtn", tab) is Button closeBtn)
            {
                closeBtn.Click += (s, args) => CloseTab(tab);
            }
        }

        private void CloseTab(TabItem tab)
        {
            MainTabControl.Items.Remove(tab);
            UpdateTabVisibility();
        }

        private void UpdateTabVisibility()
        {
            if (MainTabControl.Items.Count > 0)
            {
                MainTabControl.Visibility = Visibility.Visible;
                EmptyStateOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainTabControl.Visibility = Visibility.Hidden;
                EmptyStateOverlay.Visibility = Visibility.Visible;

                // Deshabilitar Toolbox si no hay pestañas
                _toolboxView.EnableTools(false);
            }
        }

        // Evento cuando se hace clic en un control del toolbox
        private void Toolbox_ControlSelected(object sender, string controlName)
        {
            // Aquí iría tu lógica para agregar el control al lienzo
            MessageBox.Show($"Seleccionaste: {controlName}");
        }


        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void MainWindowModern_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                BtnMaximize.Content = "\xE923"; // Icono Restaurar
            }
            else
            {
                BtnMaximize.Content = "\xE922"; // Icono Maximizar
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }


        // ============================================
        // LÓGICA DE LA BARRA DE ACTIVIDAD (Lateral Izquierda)
        // ============================================
        private ToggleButton _activeSideBarButton;

        private void ActivityBar_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as ToggleButton;
            if (clickedButton == null) return;

            string panelType = clickedButton.Tag.ToString();

            // Si el botón clickeado ya estaba activo, cerramos el panel
            if (clickedButton == _activeSideBarButton)
            {
                clickedButton.IsChecked = false;
                CloseSideBar();
                _activeSideBarButton = null;
                return;
            }

            // Si había otro botón activo, lo desactivamos visualmente
            if (_activeSideBarButton != null)
            {
                _activeSideBarButton.IsChecked = false;
            }

            // Activamos el nuevo botón y abrimos el panel correspondiente
            clickedButton.IsChecked = true;
            _activeSideBarButton = clickedButton;
            OpenSideBar(panelType);
        }

        private void OpenSideBar(string panelType)
        {
            // Establecemos un ancho fijo para el panel lateral
            SideBarContainer.Width = 250;

            switch (panelType)
            {
                case "Explorer":
                    SideBarTitle.Text = "EXPLORADOR DE PROYECTOS";
                    // Aquí cargarías tu UserControl del Explorador
                    SideBarContent.Content = new System.Windows.Controls.TextBlock();
                    SideBarContent.Content = _explorerView; // Usamos la instancia real
                    break;

                case "Toolbox":
                    SideBarTitle.Text = "CAJA DE HERRAMIENTAS";
                    // Aquí cargarías tu UserControl del Toolbox existente
                    SideBarContent.Content = _toolboxView; // Cargamos la vista
                    break;
            }
        }

        // MÉTODO SIMULADO: Llama a esto cuando el usuario cree o abra un .frm
        public void OnProjectOpened()
        {
            _toolboxView.EnableTools(true);
            // Cambiar la vista central del icono vacío al Canvas de diseño...
        }

        private void CloseSideBar()
        {
            // Colapsamos el panel
            SideBarContainer.Width = 0;
            SideBarContent.Content = null;
        }

        // 1. MENÚ ARCHIVO > NUEVO PROYECTO
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            // 1. Crear el ScrollViewer (La ventana al mundo)
            ScrollViewer scroller = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)) // Fondo gris oscuro
            };

            // 2. Crear el Lienzo de Trabajo (El área total disponible)
            // IMPORTANTE: Aquí definimos el tamaño del scrollbar
            Grid workspace = new Grid
            {
                Width = 2000,   // <--- ESTO CONTROLA EL TAMAÑO DEL SCROLL HORIZONTAL
                Height = 2000,  // <--- ESTO CONTROLA EL TAMAÑO DEL SCROLL VERTICAL
                Background = (Brush)FindResource("DotPatternBrush") // Tu patrón de puntos
            };

            // 3. Crear el "Formulario" Mockup (centrado visualmente en el workspace)
            Border mockForm = new Border
            {
                Width = 600,
                Height = 400,
                Background = Brushes.White,
                BorderBrush = Brushes.Navy,
                BorderThickness = new Thickness(2),
                // Truco para que aparezca "en medio" del lienzo grande al inicio
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            workspace.Children.Add(mockForm);
            scroller.Content = workspace;
        }

        // 2. MENÚ ARCHIVO > ABRIR
        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "VB6 Forms (*.frm)|*.frm|All Files (*.*)|*.*",
                Title = "Abrir formulario VB6 existente"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Usamos el método LoadProject que creamos antes
                LoadProject(openFileDialog.FileName);
            }
        }

        // 3. MENÚ ARCHIVO > CERRAR PROYECTO
        private void BtnCloseProject_Click(object sender, RoutedEventArgs e)
        {
            // Regresar a la pantalla de inicio
            StartScreen start = new StartScreen();
            start.Show();

            // Cerrar esta ventana de edición
            this.Close();
        }
    }


}
