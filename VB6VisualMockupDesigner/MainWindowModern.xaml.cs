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

            PropertiesPanel.CloseRequested += (s, e) => TogglePanel(PropertiesPanel, false);

            // Iniciar layout (Por defecto mostramos Propiedades, ocultamos el auxiliar)
            SecondaryPanel.Visibility = Visibility.Collapsed;
            UpdateRightLayout();
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

        private void OpenFileTab(string fullPath)
        {
            // 1. Verificar si ya está abierta
            foreach (TabItem tab in MainTabControl.Items)
            {
                // Comparamos el ToolTip o guardamos el path en el Tag para ser más precisos
                if (tab.Tag?.ToString() == fullPath)
                {
                    MainTabControl.SelectedItem = tab;
                    return;
                }
            }

            // 2. Crear nueva pestaña
            var newTab = new TabItem
            {
                Header = System.IO.Path.GetFileName(fullPath), // Solo nombre en la pestaña
                Tag = fullPath // Guardamos ruta completa en el Tag
            };

            // 3. Instanciar el Diseñador
            var designer = new DesignerCanvas();




            // === CORRECCIÓN AQUÍ ===
            // En lugar de FrmParser.Parse, leemos el archivo y usamos el método interno del designer
            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    string fileContent = System.IO.File.ReadAllText(fullPath);
                    designer.LoadForm(fileContent); // <--- ESTA ES LA CLAVE
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Error al leer el formulario: " + ex.Message);
                }
            }
            // =======================

            // Configurar título interno (Overlay del form)
            designer.FormTitle = System.IO.Path.GetFileNameWithoutExtension(fullPath);

            // Evento de selección para propiedades
            designer.ControlSelected += (s, control) =>
            {
                this.PropertiesPanel.InspectObject(control);
            };




            newTab.Content = designer;

            // 4. Agregar y seleccionar
            MainTabControl.Items.Add(newTab);
            MainTabControl.SelectedItem = newTab;

            // 5. Configurar botón de cerrar
            newTab.Loaded += NewTab_Loaded;

            UpdateTabVisibility();
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


        // ----------------------------------------------------
        // LÓGICA DE MENÚS (VER)
        // ----------------------------------------------------
        private void MnuShowProperties_Click(object sender, RoutedEventArgs e)
        {
            TogglePanel(PropertiesPanel, true);
        }

        private void MnuShowAux_Click(object sender, RoutedEventArgs e)
        {
            TogglePanel(SecondaryPanel, true);
        }

        // Botón X del panel secundario (el de propiedades usa su evento propio)
        private void BtnCloseSecondary_Click(object sender, RoutedEventArgs e)
        {
            TogglePanel(SecondaryPanel, false);
        }

        // Helper para mostrar/ocultar y recalcular
        private void TogglePanel(UIElement panel, bool show)
        {
            panel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            UpdateRightLayout();
        }

        // ----------------------------------------------------
        // EL CEREBRO DEL LAYOUT DERECHO
        // ----------------------------------------------------
        private void UpdateRightLayout()
        {
            // Validación de seguridad: Si la columna no se ha cargado, no hacemos nada.
            if (ColProperties == null) return;

            bool showProps = PropertiesPanel.Visibility == Visibility.Visible;
            bool showAux = SecondaryPanel.Visibility == Visibility.Visible;

            // 1. GESTIÓN DEL ANCHO DE LA COLUMNA
            if (!showProps && !showAux)
            {
                // Si todo está oculto, ancho 0
                ColProperties.Width = new GridLength(0);
                return;
            }
            else
            {
                // Si hay algo visible y estaba en 0, lo restauramos a 250
                if (ColProperties.Width.Value == 0)
                    ColProperties.Width = new GridLength(250);
            }

            // 2. GESTIÓN DE PANELES (ARRIBA / ABAJO)
            // Nos aseguramos que el Grid del Sidebar tenga las filas necesarias cargadas
            if (RightRow1 == null || RightRow2 == null || RightSplitterRow == null) return;

            if (showProps && showAux)
            {
                // --- AMBOS VISIBLES ---
                Grid.SetRow(PropertiesPanel, 0);
                Grid.SetRowSpan(PropertiesPanel, 1);

                Grid.SetRow(SecondaryPanel, 2);
                Grid.SetRowSpan(SecondaryPanel, 1);

                RightRow1.Height = new GridLength(1, GridUnitType.Star);
                RightRow2.Height = new GridLength(1, GridUnitType.Star);
                RightSplitterRow.Height = new GridLength(5);
            }
            else if (showProps)
            {
                // --- SOLO PROPIEDADES ---
                Grid.SetRow(PropertiesPanel, 0);
                Grid.SetRowSpan(PropertiesPanel, 3);

                RightRow1.Height = new GridLength(1, GridUnitType.Star);
                RightRow2.Height = new GridLength(0);
                RightSplitterRow.Height = new GridLength(0);
            }
            else if (showAux)
            {
                // --- SOLO AUXILIAR ---
                Grid.SetRow(SecondaryPanel, 0);
                Grid.SetRowSpan(SecondaryPanel, 3);

                RightRow1.Height = new GridLength(1, GridUnitType.Star);
                RightRow2.Height = new GridLength(0);
                RightSplitterRow.Height = new GridLength(0);
            }
        }

    }





}
