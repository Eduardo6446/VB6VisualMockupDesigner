using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Helpers;
using VB6VisualMockupDesigner.Models; // Requiere .NET Core 3.1 o superior (o NuGet en .NET Framework)
using VB6VisualMockupDesigner.Services;
using VB6VisualMockupDesigner.Views;

using static VB6VisualMockupDesigner.Controls.DesignerCanvas;



namespace VB6VisualMockupDesigner.Views
{

    /// <summary>
    /// Interaction logic for MainWindowModern.xaml
    /// </summary>
    public enum AppMode
    {
        Empty,          // Nada abierto (Pantalla vacía)
        SingleFile,     // Solo un .frm suelto
        Folder,         // Carpeta completa abierta
        ProjectVbp      // Proyecto .vbp abierto
    }

    public partial class MainWindowModern : Window
    {
        private AppMode _currentMode = AppMode.Empty;
        private ToolboxView _toolboxView; // Instancia única para no recrearla siempre
        private ProjectExplorerView _explorerView; // Nueva referencia

        private void MnuSave_Click(object sender, RoutedEventArgs e) => SaveProject(false);
        private void MnuSaveAs_Click(object sender, RoutedEventArgs e) => SaveProject(true);

        public MainWindowModern()
        {
            InitializeComponent();

            this.Title = VersionInfo.WindowTitle;

            this.WindowState = WindowState.Maximized;



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

            PropertiesPanel.PropertyChanging += (s, e) =>
            {
                if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas designer)
                {
                    designer.SaveUndoSnapshot();
                }
            };

            // 2. Después de editar: Refrescar los bordes azules en el designer activo
            PropertiesPanel.PropertyChanged += (s, e) =>
            {
                if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas designer)
                {
                    designer.RefreshSelectionVisuals();
                }
            };

            //VALIDACIÓN DE NOMBRES
            PropertiesPanel.CheckNameAvailability = (candidateName) =>
            {
                if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas designer)
                {
                    // 1. Buscamos si existe alguien con ese nombre
                    FrameworkElement existingControl = null;
                    foreach (UIElement child in designer.GetDesignSurface().Children)
                    {
                        if (child is FrameworkElement fe &&
                            string.Equals(fe.Name, candidateName, StringComparison.OrdinalIgnoreCase))
                        {
                            existingControl = fe;
                            break;
                        }
                    }

                    // 2. Si NO existe, el nombre está libre. ¡Adelante!
                    if (existingControl == null) return true;

                    // 3. Si SÍ existe, preguntamos si quiere crear Array
                    var result = MessageBox.Show(
                        $"Ya existe un control llamado '{candidateName}'.\n¿Desea crear una matriz de controles?",
                        "Visual Basic 6 Mockup",
                        MessageBoxButton.YesNo, // Solo Yes/No. Si cancela, devuelve false.
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // --- LÓGICA DE CREACIÓN DE ARRAY MANUAL ---

                        // A. Preparamos el control EXISTENTE (el que ya tenía el nombre)
                        int? idxExisting = VB6Data.GetIndex(existingControl);
                        if (idxExisting == null)
                        {
                            VB6Data.SetIndex(existingControl, 0);
                        }

                        // B. Buscamos el siguiente índice libre para este nombre
                        // (Necesitamos acceder a la función auxiliar o recalcularla aquí)
                        int maxIndex = 0;
                        if (idxExisting.HasValue) maxIndex = idxExisting.Value;

                        // Buscamos en todo el canvas otros hermanos del array para hallar el max
                        foreach (UIElement child in designer.GetDesignSurface().Children)
                        {
                            if (child is FrameworkElement fe &&
                                string.Equals(fe.Name, candidateName, StringComparison.OrdinalIgnoreCase))
                            {
                                int? idx = VB6Data.GetIndex(fe);
                                if (idx.HasValue && idx.Value > maxIndex) maxIndex = idx.Value;
                            }
                        }

                        // C. Asignamos el índice al control ACTUAL (el que estamos renombrando en el panel)
                        // OJO: El panel asignará el Nombre después de que retornemos true.
                        // Nosotros solo asignamos el Index aquí.
                        FrameworkElement currentCtrl = designer.GetSelectedControls()[0]; // El que se está editando
                        VB6Data.SetIndex(currentCtrl, maxIndex + 1);

                        return true; // Permitimos el cambio de nombre
                    }

                    // Si dijo NO, rechazamos el cambio
                    return false;
                }
                return false;
            };

            // Conectar el evento del Combo del Panel hacia el Diseñador activo
            PropertiesPanel.ObjectSelectedFromList += (controlToSelect) =>
            {
                if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas designer)
                {
                    designer.SelectControl(controlToSelect);
                }
            };

            SecondaryPanel.NodeSelected += (control) =>
            {
                if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas designer)
                {
                    designer.SelectControl(control);
                }
            };

            // Botón Cerrar del panel
            SecondaryPanel.CloseRequested += (s, e) => TogglePanel(SecondaryPanel, false);
            this.Closing += MainWindowModern_Closing; // <--- AGREGAR ESTO

        }


        private void UpdateUIContext()
        {
            switch (_currentMode)
            {
                case AppMode.Empty:
                case AppMode.SingleFile:
                    // En modo archivo suelto o vacío, NO tiene sentido cerrar carpeta/proyecto
                    if (MnuCloseFolder != null)
                    {
                        MnuCloseFolder.Header = "Cerrar Carpeta";
                        MnuCloseFolder.IsEnabled = false;
                    }
                    
                    break;

                case AppMode.Folder:
                    // Modo Carpeta estilo VS Code
                    if (MnuCloseFolder != null)
                    {
                        MnuCloseFolder.Header = "Cerrar Carpeta";
                        MnuCloseFolder.IsEnabled = true;
                    }
                    
                    break;

                case AppMode.ProjectVbp:
                    // Modo Proyecto VB6 clásico
                    if (MnuCloseFolder != null)
                    {
                        MnuCloseFolder.Header = "Cerrar Proyecto"; // <--- Cambio dinámico de texto
                        MnuCloseFolder.IsEnabled = true;
                    }
                    
                    break;
            }
        }

        private void RefreshDocumentOutline()
        {
            if (SecondaryPanel.Visibility == Visibility.Visible &&
                MainTabControl.SelectedItem is TabItem tab &&
                tab.Content is DesignerCanvas designer)
            {
                SecondaryPanel.LoadHierarchy(designer);
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl && MainTabControl.SelectedItem is TabItem selectedTab)
            {
                // Obtenemos la ruta completa del Tag (que guardamos al crear la pestaña)
                if (selectedTab.Tag is string fullPath)
                {
                    // ¡Sincronizamos el explorador!
                    _explorerView.SelectFile(fullPath);
                }

                ZoomSlider.Value = 100;

                // LÓGICA DE ACTIVACIÓN CORREGIDA
                // Solo habilitamos el botón si es un DesignerCanvas
                if (selectedTab.Content is DesignerCanvas)
                {
                    UpdateToolboxState(true);
                }
                else
                {
                    UpdateToolboxState(false);
                }
            }
            RefreshDocumentOutline();

        }

        // Helper para gestionar el estado de la Toolbox
        private void UpdateToolboxState(bool isVisible)
        {
            if (BtnToolboxToggle == null) return;

            // 1. Habilitar o deshabilitar el botón
            BtnToolboxToggle.IsEnabled = isVisible;

            // 2. Controlar el contenido interno (por si acaso)
            _toolboxView.EnableTools(isVisible);

            // 3. SI DESHABILITAMOS: Cerrar el panel si estaba abierto
            if (!isVisible && BtnToolboxToggle.IsChecked == true)
            {
                BtnToolboxToggle.IsChecked = false;
                CloseSideBar();
                _activeSideBarButton = null; // Resetear referencia
            }
        }

        public void LoadProject(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            if (Directory.Exists(path))
            {
                _currentMode = AppMode.Folder; // <--- SET MODE
                _explorerView?.LoadFolderContents(path);
                OpenSideBar("Explorer");
            }
            else
            {
                string ext = System.IO.Path.GetExtension(path).ToLower();
                if (ext == ".vbp")
                {
                    _currentMode = AppMode.ProjectVbp; // <--- SET MODE
                    _explorerView?.LoadProjectStructure(path);
                    OpenSideBar("Explorer");
                }
                else
                {
                    // Si ya estamos en modo Folder o Project, NO cambiamos a SingleFile
                    // solo porque abrimos un archivo. Mantenemos el contexto.
                    if (_currentMode == AppMode.Empty)
                    {
                        _currentMode = AppMode.SingleFile; // <--- SET MODE
                    }

                    OpenFileTab(path);
                }
            }

            RecentFilesManager.AddToRecents(path);
            UpdateUIContext(); // <--- ACTUALIZAR BOTONES
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
            // 1. Verificar si ya está abierta la pestaña
            foreach (TabItem tab in MainTabControl.Items)
            {
                if (tab.Tag?.ToString() == fullPath)
                {
                    MainTabControl.SelectedItem = tab;
                    return;
                }
            }

            string fileName = System.IO.Path.GetFileName(fullPath);
            string ext = System.IO.Path.GetExtension(fullPath).ToLower();

            // 2. Crear nueva pestaña
            var newTab = new TabItem
            {
                Header = fileName,
                Tag = fullPath
            };

            // 3. DECIDIR CONTENIDO SEGÚN EXTENSIÓN
            if (ext == ".frm")
            {
                // === MODO DISEÑO (.FRM) ===
                // Aquí SÍ cargamos el DesignerCanvas, lo que habilitará la Toolbox
                var designer = new DesignerCanvas();

                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        string fileContent = System.IO.File.ReadAllText(fullPath);
                        VbControlModel rootModel = Vb6Helpers.ParseVb6Form(fileContent);
                        designer.LoadForm(fileContent);

                        if (rootModel.Menus != null && rootModel.Menus.Count > 0)
                        {
                            designer.RenderMenus(rootModel.Menus);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al leer el formulario: " + ex.Message);
                    }
                }

                designer.FormTitle = System.IO.Path.GetFileNameWithoutExtension(fullPath);
                designer.IsDirtyChanged += Designer_IsDirtyChanged;

                // Conectar eventos del panel de propiedades
                designer.ControlSelected += (s, control) =>
                {
                    this.PropertiesPanel.InspectObject(control);
                    if (BtnLockToggle != null) BtnLockToggle.IsChecked = designer.IsSelectionLocked();
                };

                newTab.Content = designer;
            }
            else
            {
                // === MODO CÓDIGO (.BAS, .CLS, .VBP, etc.) ===
                // Aquí cargamos un TextBox simple. La Toolbox se mantendrá DESACTIVADA.
                TextBox codeView = new TextBox
                {
                    AcceptsReturn = true,
                    AcceptsTab = true,
                    FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                    FontSize = 13,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    // Usamos tus recursos de color para que se vea integrado
                    Background = (Brush)Application.Current.Resources["AppBackground"],
                    Foreground = (Brush)Application.Current.Resources["PrimaryText"],
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10),
                    Text = System.IO.File.Exists(fullPath) ? System.IO.File.ReadAllText(fullPath) : "",
                    IsReadOnly = false // Puedes poner true si solo quieres visualizar
                };

                newTab.Content = codeView;
            }

            // 4. Configurar botón de cerrar y agregar
            newTab.Loaded += NewTab_Loaded;
            MainTabControl.Items.Add(newTab);
            MainTabControl.SelectedItem = newTab;

            // Esto llamará a UpdateToolboxState automáticamente gracias al evento SelectionChanged
            UpdateTabVisibility();
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
            // 1. Verificar si hay cambios sin guardar
            if (tab.Content is DesignerCanvas designer && designer.IsDirty)
            {
                string docName = tab.Header.ToString().TrimEnd('*');

                var result = MessageBox.Show(
                    $"¿Desea guardar los cambios en '{docName}'?",
                    "Guardar cambios",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel) return; // Cancelar cierre

                if (result == MessageBoxResult.Yes)
                {
                    // Intentar guardar. Si falla o cancela el diálogo de archivo, abortamos cierre.
                    // (Necesitamos seleccionar la pestaña temporalmente para que SaveProject funcione sobre ella)
                    MainTabControl.SelectedItem = tab;
                    SaveProject(false);

                    // Verificamos si realmente se guardó (se limpió el Dirty)
                    if (designer.IsDirty) return;
                }
            }

            // 2. Proceder al cierre
            MainTabControl.Items.Remove(tab);
            UpdateTabVisibility();
        }

        private void UpdateTabVisibility()
        {
            bool hasTabs = MainTabControl.Items.Count > 0;

            if (MainTabControl.Items.Count > 0)
            {
                MainTabControl.Visibility = Visibility.Visible;
                EmptyStateOverlay.Visibility = Visibility.Collapsed;

                // Validamos la pestaña actual por si acaso
                if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas)
                {
                    UpdateToolboxState(true);
                }
            }
            else
            {
                MainTabControl.Visibility = Visibility.Hidden;
                EmptyStateOverlay.Visibility = Visibility.Visible;

                // NO HAY PESTAÑAS -> DESHABILITAR TOTALMENTE
                UpdateToolboxState(false);
            }

            if (!hasTabs && _currentMode == AppMode.SingleFile)
            {
                _currentMode = AppMode.Empty;
                UpdateUIContext();
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
                    // SideBarTitle.Text = "EXPLORADOR DE PROYECTOS"; // <-- LÍNEA ELIMINADA

                    // Asignamos la vista directamente
                    SideBarContent.Content = _explorerView;
                    break;

                case "Toolbox":
                    // SideBarTitle.Text = "CAJA DE HERRAMIENTAS"; // <-- LÍNEA ELIMINADA

                    // Asignamos la vista directamente
                    SideBarContent.Content = _toolboxView;
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
            // 1. Generar un nombre temporal (Form1, Form2, etc.)
            // Aquí podrías iterar para buscar el siguiente libre, por ahora usaremos Form1
            string defaultName = "Form1";

            // 2. Crear nueva pestaña
            var newTab = new TabItem
            {
                Header = defaultName,
                Tag = null // IMPORTANTE: Tag es null porque aún no existe el archivo en disco
            };

            // 3. Instanciar el Diseñador (Usamos DesignerCanvas, NO un Grid suelto)
            var designer = new DesignerCanvas();
            designer.FormTitle = defaultName;

            designer.SetFormDimensions(600, 450);

            designer.IsDirtyChanged += Designer_IsDirtyChanged;

            // 4. Configurar eventos (Igual que en OpenFileTab)
            // Esto es vital para que el panel de propiedades funcione con el nuevo form
            designer.ControlSelected += (s, control) =>
            {
                // 1. Inspeccionar propiedades
                this.PropertiesPanel.InspectObject(control);

                // 2. Actualizar estado del botón de bloqueo
                if (BtnLockToggle != null)
                {
                    BtnLockToggle.IsChecked = designer.IsSelectionLocked();
                }

                // 3. NUEVO: Actualizar la lista del ComboBox "Object Selector"
                // Obtenemos todos los hijos visuales y filtramos basura (Handles, Bordes azules, etc.)
                var allControls = designer.GetDesignSurface().Children.OfType<FrameworkElement>()
                    .Where(c => c.Name != "SelectionRect"
                             && c.Name != "QuickEditBox"
                             && !(c is System.Windows.Shapes.Rectangle) // Ignorar handles de redimensión
                             && !(c is Border)); // Ignorar bordes de selección

                this.PropertiesPanel.UpdateObjectList(allControls, control as FrameworkElement);
                RefreshDocumentOutline();
            };

            // 5. Asignar el diseñador a la pestaña
            newTab.Content = designer;

            // 6. Agregar al TabControl y Seleccionar
            MainTabControl.Items.Add(newTab);
            MainTabControl.SelectedItem = newTab;

            // 7. Configurar botón de cerrar pestaña
            newTab.Loaded += NewTab_Loaded;

            // 8. Actualizar visibilidad y habilitar herramientas
            UpdateTabVisibility();
            _toolboxView.EnableTools(true);
            designer.MarkAsClean(); // Marcamos como limpio al inicio
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

        // Helper para refrescar el árbol si está visible
        private void UpdateDocumentOutline()
        {
            if (SecondaryPanel.Visibility == Visibility.Visible &&
                MainTabControl.SelectedItem is TabItem tab &&
                tab.Content is DesignerCanvas designer)
            {
                SecondaryPanel.LoadHierarchy(designer);
            }
        }

        private void MnuShowAux_Click(object sender, RoutedEventArgs e)
        {
            TogglePanel(SecondaryPanel, true);
            UpdateDocumentOutline(); // <--- AGREGAR ESTO PARA QUE CARGUE AL ABRIR
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
            if (show && panel == SecondaryPanel) RefreshDocumentOutline(); // <--- AGREGAR
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


        

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            var tb = e.OriginalSource as TextBox;

            if (tb != null || e.OriginalSource is PasswordBox)
            {


                if (IsDescendant((DependencyObject)e.OriginalSource, PropertiesPanel))
                {
                    return; 
                }


                if (tb != null && MainTabControl.SelectedContent == tb)
                {
                    return; // Bloquear atajo global, es código fuente.
                }

            }



            // Verificamos si hay un diseñador activo
            if (!(MainTabControl.SelectedItem is TabItem tab) || !(tab.Content is DesignerCanvas designer))
                return;

            // Detectar Control presionado
            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (isCtrl && isShift && e.Key == Key.C)
            {
                MnuCopyClipboard_Click(null, null);
                e.Handled = true;
                return;
            }

            // 1. SUPRIMIR (Delete)
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                designer.DeleteSelectedControl();
            }


            // 2. COMBINACIONES CON CTRL
            if (isCtrl)
            {
                switch (e.Key)
                {
                    case Key.C: // COPIAR
                        designer.CopySelected();
                        break;

                    case Key.X: // CORTAR
                        designer.CutSelected();
                        break;

                    case Key.V: // PEGAR
                        designer.Paste();
                        break;

                    case Key.Z: // DESHACER
                        designer.Undo();
                        break;

                    case Key.Y: // REHACER
                        designer.Redo();
                        break;

                    case Key.S: // GUARDAR
                        SaveProject(false);
                        e.Handled = true; // Evitar bubbling
                        break;
                }

            }

            // 3. GLOBAL SEARCH (Ctrl + P)
            if (isCtrl && e.Key == Key.P)
            {
                TxtGlobalSearch.Focus();
                e.Handled = true;
            }

            // 4. LOCK / UNLOCK SHORTCUTS
            if (isCtrl && e.Key == Key.L)
            {
                if (isShift)
                    designer.UnlockSelected(); // Ctrl + Shift + L
                else
                    designer.LockSelected();   // Ctrl + L

                e.Handled = true;
            }

            else if (!isCtrl && (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down))
            {
                // Definir velocidad
                // Normal: 1 pixel (precisión)
                // Shift: 10 pixeles (Grid grande / Rápido)
                double step = isShift ? 10.0 : 1.0;

                double dx = 0;
                double dy = 0;

                switch (e.Key)
                {
                    case Key.Left: dx = -step; break;
                    case Key.Right: dx = step; break;
                    case Key.Up: dy = -step; break;
                    case Key.Down: dy = step; break;
                }

                // Ejecutar movimiento
                designer.NudgeSelection(dx, dy);

                // IMPORTANTE: Handled = true evita que el ScrollViewer se mueva cuando presionas flechas
                e.Handled = true;
            }





        }


        // Helper para verificar si un control está dentro de otro contenedor
        private bool IsDescendant(DependencyObject node, DependencyObject container)
        {
            if (node == null || container == null) return false;

            try
            {
                DependencyObject current = node;
                while (current != null)
                {
                    if (current == container) return true;

                    // Subir un nivel en el árbol visual
                    current = VisualTreeHelper.GetParent(current);
                }
            }
            catch
            {
                // Ignorar errores de threading o visual tree desconectado 
            }
            return false;
        }

        private void SaveProject(bool forceSaveAs)
        {
            // 1. Validar que haya algo abierto
            if (!(MainTabControl.SelectedItem is TabItem tab) || !(tab.Content is DesignerCanvas designer))
            {
                MessageBox.Show("No hay ningún formulario abierto para guardar.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 2. Generar el código VB6
            string vbCode = Vb6Generator.GenerateFrmCode(designer.GetDesignSurface(), designer.FormTitle, designer.CurrentMenus);
            // 3. Determinar la ruta destino
            string currentPath = tab.Tag as string;
            string targetPath = currentPath;

            // Abrimos diálogo si: Forzamos "Guardar Como" O no tenemos ruta guardada
            if (forceSaveAs || string.IsNullOrEmpty(currentPath))
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "VB6 Form (*.frm)|*.frm|All Files (*.*)|*.*",
                    FileName = designer.FormTitle + ".frm",
                    Title = forceSaveAs ? "Guardar copia como..." : "Guardar proyecto"
                };

                if (sfd.ShowDialog() == true)
                {
                    targetPath = sfd.FileName;

                    // Actualizamos la pestaña con los nuevos datos
                    tab.Tag = targetPath;
                    tab.Header = System.IO.Path.GetFileName(targetPath);
                    designer.FormTitle = System.IO.Path.GetFileNameWithoutExtension(targetPath);
                }
                else
                {
                    return; // Usuario canceló
                }
            }

            // 4. Escribir en disco
            try
            {
                File.WriteAllText(targetPath, vbCode);

                // Feedback visual en la barra de estado (Opcional, si tienes una)
                // StatusBarText.Text = $"Guardado: {System.DateTime.Now.ToShortTimeString()}";

                designer.MarkAsClean();

                // Solo mostramos MessageBox si fue "Guardar Como" para confirmar
                if (forceSaveAs)
                    MessageBox.Show("Formulario guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MnuExportPng_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validar que haya un diseñador activo
            if (!(MainTabControl.SelectedItem is TabItem tab) || !(tab.Content is DesignerCanvas designer))
            {
                MessageBox.Show("No hay ningún diseño abierto para exportar.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 2. Configurar el diálogo de guardado
            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png",
                FileName = designer.FormTitle + ".png", // Sugerir el nombre del form
                Title = "Exportar Mockup a Imagen"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    // 3. Llamar al método del canvas
                    designer.SaveAsImage(sfd.FileName);

                    // Opcional: Abrir la imagen automáticamente o mostrar mensaje
                    //System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                    MessageBox.Show("Imagen exportada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error al exportar imagen: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ==========================================
        // NUEVO: COPIAR IMAGEN AL PORTAPAPELES
        // ==========================================
        // En Views/MainWindowModern.xaml.cs

        private void MnuCopyClipboard_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validar que haya un diseñador activo
            if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas designer)
            {
                // 2. Llamar al nuevo método del canvas
                designer.CopyToClipboard();

                // 3. Feedback visual opcional
                // (Como no tenemos StatusBar message method aun, un MessageBox discreto o nada está bien)
                // MessageBox.Show("Copiado al portapapeles.", "Información", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // 1. Evento al escribir texto
        private void TxtGlobalSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = TxtGlobalSearch.Text;

            if (string.IsNullOrWhiteSpace(query))
            {
                SearchPopup.IsOpen = false;
                return;
            }

            // Buscamos usando el método que creamos en el Paso 1
            var results = _explorerView.SearchFiles(query);

            if (results.Count > 0)
            {
                LstSearchResults.ItemsSource = results;
                SearchPopup.IsOpen = true;

                // Seleccionamos el primero por defecto para navegación rápida
                LstSearchResults.SelectedIndex = 0;
            }
            else
            {
                SearchPopup.IsOpen = false;
            }
        }

        // 2. Manejo de Teclas en el TextBox (Flechas y Enter)
        private void TxtGlobalSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!SearchPopup.IsOpen) return;

            if (e.Key == Key.Down)
            {
                // Mover foco a la lista
                LstSearchResults.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                // Ejecutar selección actual
                ConfirmSearchSelection();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                SearchPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        // 3. Manejo de Teclas en la Lista (Enter para seleccionar)
        private void LstSearchResults_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ConfirmSearchSelection();
                e.Handled = true;
            }
            else if (e.Key == Key.Up && LstSearchResults.SelectedIndex == 0)
            {
                // Si estamos arriba del todo y subimos, volver al textbox
                TxtGlobalSearch.Focus();
            }
        }

        // 4. Click con el mouse
        private void LstSearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ConfirmSearchSelection();
        }

        // 5. Lógica central de confirmación
        private void ConfirmSearchSelection()
        {
            if (LstSearchResults.SelectedItem is ExplorerItem selectedItem)
            {
                // 1. Cerrar popup
                SearchPopup.IsOpen = false;
                TxtGlobalSearch.Text = ""; // Limpiar búsqueda

                // 2. USAR LA SINCRONIZACIÓN QUE YA CREAMOS
                // Esto automáticamente:
                //    a) Buscará el archivo en el árbol
                //    b) Expandirá las carpetas
                //    c) Lo seleccionará visualmente
                //    d) Disparará el evento OnSelectedItemChanged
                //    e) Que a su vez llamará a OpenFileTab

                _explorerView.SelectFile(selectedItem.FullPath);

                OpenFileTab(selectedItem.FullPath);

                // Foco al editor (opcional)
                MainTabControl.Focus();
            }
        }



        // ==========================================
        // LÓGICA DE LA TOOLBAR
        // ==========================================

        // Helper para ejecutar acciones en la pestaña activa
        private void ExecuteOnActiveDesigner(Action<DesignerCanvas> action)
        {
            if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas designer)
            {
                action(designer);
            }
        }

        // 1. UNDO / REDO
        private void BtnUndo_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.Undo());
        private void BtnRedo_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.Redo());

        // 2. CLIPBOARD
        private void BtnCut_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.CutSelected());
        private void BtnCopy_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.CopySelected());
        private void BtnPaste_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.Paste());
        private void BtnDelete_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.DeleteSelectedControl());

        // 3. ALINEACIÓN
        private void BtnAlignLeft_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.AlignSelected("Left"));
        private void BtnAlignCenter_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.AlignSelected("Center"));
        private void BtnAlignRight_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.AlignSelected("Right"));

        private void BtnAlignTop_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.AlignSelected("Top"));
        private void BtnAlignMiddle_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.AlignSelected("Middle"));
        private void BtnAlignBottom_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.AlignSelected("Bottom"));

        // 4. TAMAÑO
        private void BtnSameWidth_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.AlignSelected("SameWidth"));
        private void BtnSameHeight_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.AlignSelected("SameHeight"));

        // 5. ORDENAMIENTO (Z-ORDER)
        private void BtnBringToFront_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.BringToFront());
        private void BtnSendToBack_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.SendToBack());

        private void BtnTabOrder_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.ToggleTabOrderMode());

        private void BtnLock_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.LockSelected());
        private void BtnUnlock_Click(object sender, RoutedEventArgs e) => ExecuteOnActiveDesigner(d => d.UnlockSelected());

        private void BtnLockToggle_Click(object sender, RoutedEventArgs e)
        {
            bool wantLock = BtnLockToggle.IsChecked == true;

            ExecuteOnActiveDesigner(d =>
            {
                if (wantLock)
                    d.LockSelected();
                else
                    d.UnlockSelected();
            });
        }
        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow about = new AboutWindow();
            about.Owner = this;
            about.ShowDialog();
        }

        private void Lienzo_MouseMove(object sender, MouseEventArgs e)
        {
            // Obtener la posición del mouse relativa al contenedor (sender)
            Point p = e.GetPosition((IInputElement)sender);

            txtCursorPos.Text = $"X: {(int)p.X},  Y: {(int)p.Y}";
        }

        private void Lienzo_MouseLeave(object sender, MouseEventArgs e)
        {
            txtCursorPos.Text = "X: 0, Y: 0"; // O dejarlo vacío
        }

        // Busca este método y reemplázalo completamente
        private void MnuPreferences_Click(object sender, RoutedEventArgs e)
        {
            // 1. Verificar si la pestaña de Configuración ya está abierta
            foreach (TabItem item in MainTabControl.Items)
            {
                // Usamos el Tag para identificarla de forma única
                if (item.Tag != null && item.Tag.ToString() == "SETTINGS_TAB")
                {
                    MainTabControl.SelectedItem = item;
                    return; // Ya existe, solo la enfocamos
                }
            }

            // 2. Crear la vista
            var settingsView = new SettingsView();

            // 3. Crear la pestaña
            var newTab = new TabItem
            {
                Header = "Preferencias",
                Tag = "SETTINGS_TAB", // Identificador único
                Content = settingsView
            };


            // 5. Configurar el botón de cerrar de la pestaña (la X pequeña del header)
            newTab.Loaded += NewTab_Loaded;

            // 6. Agregar y enfocar
            MainTabControl.Items.Add(newTab);
            MainTabControl.SelectedItem = newTab;

            // 7. Actualizar UI (Toolbox, etc.)
            UpdateTabVisibility();

            // Como es configuración, nos aseguramos que la toolbox esté deshabilitada
            UpdateToolboxState(false);
        }

        // Evento del Slider de Zoom
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtZoomLevel != null)
            {
                TxtZoomLevel.Text = $"{(int)e.NewValue}%";
            }

            if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas designer)
            {
                // Pasamos null para que use el centro de la pantalla
                designer.SetZoom(e.NewValue, null);
            }
        }

        // ==========================================
        // ZOOM CON RUEDA DEL MOUSE (Ctrl + Wheel)
        // ==========================================
        private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas designer)
                {
                    // 1. Obtener posición del mouse RELATIVA AL AREA DE DIBUJO VISIBLE
                    // Usamos el designer como referencia, pero el cálculo interno del canvas usa el ScrollViewer
                    // Lo ideal es pasar la posición relativa al UserControl del designer.
                    Point mousePos = e.GetPosition(designer);

                    // 2. Calcular nuevo valor
                    double step = 10;
                    double newVal = ZoomSlider.Value;

                    if (e.Delta > 0) newVal += step;
                    else newVal -= step;

                    // 3. Validar límites manualmente (porque no estamos usando el slider directamente todavía)
                    if (newVal < ZoomSlider.Minimum) newVal = ZoomSlider.Minimum;
                    if (newVal > ZoomSlider.Maximum) newVal = ZoomSlider.Maximum;

                    // 4. Actualizar el slider (esto disparará ValueChanged)
                    // TRUCO: Desuscribimos momentáneamente el evento del slider para llamar a SetZoom nosotros mismos con el mousePos
                    ZoomSlider.ValueChanged -= ZoomSlider_ValueChanged;
                    ZoomSlider.Value = newVal;
                    ZoomSlider.ValueChanged += ZoomSlider_ValueChanged;

                    // Actualizar texto
                    if (TxtZoomLevel != null) TxtZoomLevel.Text = $"{(int)newVal}%";

                    // 5. Llamar al Zoom con la posición del mouse
                    designer.SetZoom(newVal, mousePos);
                }

                e.Handled = true;
            }
        }


        // ==========================================
        // GRID Y SNAPPING
        // ==========================================

        private void BtnToggleSnap_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOnActiveDesigner(d => d.ToggleSnapping());
        }

        private void BtnToggleGrid_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOnActiveDesigner(d => d.ToggleGrid());
        }



        // ==========================================
        // DISTRIBUCIÓN
        // ==========================================

        private void BtnDistributeHoriz_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOnActiveDesigner(d => d.DistributeSelected("Horizontal"));
        }

        private void BtnDistributeVert_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOnActiveDesigner(d => d.DistributeSelected("Vertical"));
        }


        // Actualiza el título de la pestaña cuando cambia el estado Dirty
        private void Designer_IsDirtyChanged(object sender, EventArgs e)
        {
            if (sender is DesignerCanvas designer)
            {
                foreach (TabItem tab in MainTabControl.Items)
                {
                    if (tab.Content == designer)
                    {
                        string currentHeader = tab.Header.ToString();

                        // AQUÍ ES DONDE SE MODIFICA VISUALMENTE EL ASTERISCO
                        if (designer.IsDirty) // <--- Llama a tu nueva lógica de IDs
                        {
                            if (!currentHeader.EndsWith("*"))
                                tab.Header = currentHeader + "*";
                        }
                        else
                        {
                            if (currentHeader.EndsWith("*"))
                                tab.Header = currentHeader.TrimEnd('*');
                        }
                        break;
                    }
                }
            }
        }


        private void MainWindowModern_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Recorremos todas las pestañas buscando cambios sin guardar
            foreach (TabItem tab in MainTabControl.Items)
            {
                if (tab.Content is DesignerCanvas designer && designer.IsDirty)
                {
                    // Seleccionamos la pestaña sucia para que el usuario vea qué es
                    MainTabControl.SelectedItem = tab;
                    string docName = tab.Header.ToString().TrimEnd('*');

                    var result = MessageBox.Show(
                        $"Hay cambios sin guardar en '{docName}'.\n\n¿Desea guardarlos antes de salir?",
                        "Salir de la aplicación",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true; // ABORTAR EL CIERRE DE LA APP
                        return;
                    }

                    if (result == MessageBoxResult.Yes)
                    {
                        SaveProject(false);
                        // Si después de intentar guardar sigue sucio (ej: canceló el SaveDialog), abortamos
                        if (designer.IsDirty)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                    // Si dijo No, seguimos al siguiente loop (o cerramos si era el último)
                }
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            bool isFile = e.Data.GetDataPresent(DataFormats.FileDrop);

            if (isFile)
            {
                // Opcional: Podríamos validar la extensión aquí mismo para ser más estrictos
                // pero con verificar que sea un archivo es suficiente para el feedback visual.
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true; // Importante: Decimos que ya manejamos el evento
        }

        // Evento lógico: Procesa el archivo cuando el usuario lo suelta
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Obtener lista de archivos arrastrados (pueden ser varios)
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files != null && files.Length > 0)
                {
                    // Por simplicidad, tomamos el primero. 
                    // Si quisieras soportar abrir múltiples, podrías iterar.
                    string fileToOpen = files[0];
                    string ext = System.IO.Path.GetExtension(fileToOpen).ToLower();

                    // Validamos que sea un archivo que entendemos
                    if (ext == ".frm" || ext == ".vbp")
                    {
                        // ¡Reutilizamos tu lógica existente!
                        LoadProject(fileToOpen);
                    }
                    else
                    {
                        MessageBox.Show("Solo se admiten archivos de proyecto (.vbp) o formularios (.frm).",
                                        "Formato no soportado",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }



        public List<MenuModel> CurrentMenus { get; set; } = new List<MenuModel>();

        private void BtnMenuEditor_Click(object sender, RoutedEventArgs e)
        {
            // Solo abrir si hay un diseñador activo
            if (MainTabControl.SelectedItem is TabItem tab && tab.Content is DesignerCanvas designer)
            {
                // 1. Crear la ventana

                var editor = new MenuEditorWindow();
                editor.Owner = this; // Para que se centre sobre la ventana principal


                if (designer.CurrentMenus != null && designer.CurrentMenus.Count > 0)
                {
                    foreach (var m in designer.CurrentMenus)
                    {
                        editor.MenuItems.Add(new MenuModel
                        {
                            Caption = m.Caption,
                            Name = m.Name,
                            Level = m.Level,
                            Enabled = m.Enabled,
                            Visible = m.Visible,
                            Checked = m.Checked,
                            Shortcut = m.Shortcut
                        });
                    }
                }
                else
                {
                    // Si no hay nada, asegurar que haya al menos un item vacío como antes
                    editor.MenuItems.Add(new MenuModel { Caption = "", Name = "" });
                }



                // 2. Mostrar como Modal (bloquea la ventana de atrás)
                if (editor.ShowDialog() == true)
                {
                    // 2. ACTUALIZAR Y RENDERIZAR
                    var newMenus = editor.MenuItems.ToList();
                    designer.RenderMenus(newMenus);

                    // Marcar como sucio para el guardado
                    designer.SaveUndoSnapshot();
                }
            }
        }

        // Evento para convertir el scroll vertical de la rueda en scroll horizontal para las pestañas
        private void TabScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                // Si la rueda va hacia arriba (Delta > 0), movemos a la izquierda
                if (e.Delta > 0)
                    scrollViewer.LineLeft();
                // Si la rueda va hacia abajo (Delta < 0), movemos a la derecha
                else
                    scrollViewer.LineRight();

                e.Handled = true; // Detenemos el evento para que no afecte a otros controles
            }
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            // Si ya hay una carpeta abierta, preguntamos si cerrar primero
            if (MainTabControl.Items.Count > 0 || (_explorerView != null && _explorerView.HasContent)) // HasContent es opcional, puedes chequear ItemsSource != null
            {
                if (!CloseAllTabsAndFolder()) return; // Si el usuario cancela, no abrimos la nueva
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Seleccionar carpeta actual",
                Title = "Abrir Carpeta de Proyecto",
                Filter = "Carpetas|*.folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    LoadProject(folderPath);
                }
            }
        }

        // 2. NUEVO: MENÚ CERRAR CARPETA
        private void BtnCloseFolder_Click(object sender, RoutedEventArgs e)
        {
            CloseAllTabsAndFolder();
        }

        // 3. HELPER: CIERRA TODO Y LIMPIA
        private bool CloseAllTabsAndFolder()
        {
            // ... (Tu lógica de cerrar pestañas igual que antes) ...
            var tabs = MainTabControl.Items.Cast<TabItem>().ToList();
            foreach (var tab in tabs)
            {
                // ... lógica de guardado ...
                MainTabControl.SelectedItem = tab;
                if (tab.Content is DesignerCanvas designer && designer.IsDirty)
                {
                    // ... MessageBox ...
                    // si cancela return false;
                }
                MainTabControl.Items.Remove(tab);
            }

            // LIMPIEZA FINAL
            _explorerView.Clear();
            CloseSideBar();
            _activeSideBarButton = null;
            if (BtnToolboxToggle != null) BtnToolboxToggle.IsChecked = false;

            UpdateTabVisibility();

            // RESETEAR MODO
            _currentMode = AppMode.Empty;
            UpdateUIContext(); // <--- Deshabilita los botones de nuevo

            return true;
        }


    }
}
