using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static VB6VisualMockupDesigner.Controls.DesignerCanvas;
using VB6VisualMockupDesigner.Models; // Requiere .NET Core 3.1 o superior (o NuGet en .NET Framework)
using VB6VisualMockupDesigner.Helpers;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Services;



namespace VB6VisualMockupDesigner.Views
{

    /// <summary>
    /// Interaction logic for MainWindowModern.xaml
    /// </summary>
    public partial class MainWindowModern : Window
    {
        private ToolboxView _toolboxView; // Instancia única para no recrearla siempre
        private ProjectExplorerView _explorerView; // Nueva referencia

        private void MnuSave_Click(object sender, RoutedEventArgs e) => SaveProject(false);
        private void MnuSaveAs_Click(object sender, RoutedEventArgs e) => SaveProject(true);

        public MainWindowModern()
        {
            InitializeComponent();

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



        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.OriginalSource is TextBox || e.OriginalSource is PasswordBox)
            {
                return;
            }



            // Verificamos si hay un diseñador activo
            if (!(MainTabControl.SelectedItem is TabItem tab) || !(tab.Content is DesignerCanvas designer))
                return;

            // Detectar Control presionado
            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

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
            string vbCode = Vb6Generator.GenerateFrmCode(designer.GetDesignSurface(), designer.FormTitle);

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

        private void MnuPreferences_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para mostrar SettingsView. 
            // Como tu diseño usa pestañas o paneles laterales, puedes cargarlo ahí.
            // Ejemplo rápido: abrirlo en una ventana modal o en una nueva pestaña

            Window settingsWindow = new Window
            {
                Title = "Preferencias",
                Content = new SettingsView(), // Tu UserControl
                Width = 650,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Style = null, // Para usar ventana estándar por ahora
                WindowStyle = WindowStyle.None, // Quita la barra de Windows
                AllowsTransparency = false, // Mantenemos opacidad
                BorderThickness = new Thickness(1), // Un borde fino
                                                    // El color del borde usa el recurso dinámico del tema actual
                BorderBrush = (System.Windows.Media.Brush)Application.Current.Resources["BrandColor"],
                Background = (System.Windows.Media.Brush)Application.Current.Resources["AppBackground"]
            };
            settingsWindow.ShowDialog();
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


    }
}
