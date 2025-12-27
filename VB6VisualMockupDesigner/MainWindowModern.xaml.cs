using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;



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

            // 1. Obtener solo el nombre del archivo para el título de la pestaña
            string fileName = System.IO.Path.GetFileName(fullPath);

            // 2. Abrir la pestaña de diseño (Usando el método que creamos en el paso anterior)
            OpenFileTab(fileName);

            // 3. Registrar en archivos recientes (Persistencia JSON)
            // Asegúrate de tener la clase RecentFilesManager que creamos antes
            RecentFilesManager.AddToRecents(fullPath);

            // 4. (Opcional) Simular que el explorador selecciona este archivo
            // Esto es visual, para que coincida la pestaña con el árbol
            _explorerView.SelectFile(fileName); // Implementaremos esto si quieres detalle fino
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

        // ============================================
        // LÓGICA DE LA BARRA DE TÍTULO PERSONALIZADA
        // ============================================
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
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
                BtnMaximize.Content = "\xE922"; // Icono Maximizar
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                BtnMaximize.Content = "\xE923"; // Icono Restaurar
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
            // Por ahora, "Nuevo Proyecto" creará un formulario en blanco por defecto
            // Puedes mejorar esto luego para limpiar todo el entorno si ya había algo abierto
            LoadProject("Form1.frm");
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
