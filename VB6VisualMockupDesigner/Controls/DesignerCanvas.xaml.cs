using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VB6VisualMockupDesigner.Helpers; // Para Vb6Helpers
using VB6VisualMockupDesigner.Models;  // Para VbControlModel


namespace VB6VisualMockupDesigner.Controls
{
    /// <summary>
    /// Interaction logic for DesignerCanvas.xaml
    /// Clase Principal (Contiene Carga, Renderizado y Limpieza)
    /// </summary>
    public partial class DesignerCanvas : UserControl
    {

        public event EventHandler IsDirtyChanged;
        public bool IsRunMode { get; private set; } = false;
        private Visibility _previousGridVisibility;

        // Identificador de la versión actual en pantalla
        private Guid _currentVersionId = Guid.NewGuid();

        // Identificador de la versión que está guardada en disco
        // Al inicio son iguales (recién creado/cargado = limpio)
        private Guid _savedVersionId;


        public DesignerCanvas()
        {
            InitializeComponent();
            this.Loaded += (s, e) => { CenterView(); };
            _savedVersionId = _currentVersionId; // Sincronizamos al nacer
            SaveUndoSnapshot();

        }

        public event EventHandler<FrameworkElement> ControlSelected;

        public string FormTitle
        {
            get { return FormTitleText.Text; }
            set { FormTitleText.Text = value; }
        }

        public Canvas GetDesignSurface()
        {
            return DesignSurface;
        }

        private void CenterView()
        {
            if (MainScrollViewer == null || DesignGrid == null || MainScrollViewer.ViewportWidth == 0) return;
            double hOff = (DesignGrid.Width / 2) - (MainScrollViewer.ViewportWidth / 2);
            double vOff = (DesignGrid.Height / 2) - (MainScrollViewer.ViewportHeight / 2);
            MainScrollViewer.ScrollToHorizontalOffset(hOff);
            MainScrollViewer.ScrollToVerticalOffset(vOff);
        }

        // ============================
        // LÓGICA DE CENTRADO (NUEVO)
        // ============================
        public void CenterFormOnCanvas()
        {
            // Validamos que los controles existan (vienen del XAML)
            if (MainScrollViewer == null || DesignGrid == null || WindowResizerGrid == null) return;

            // 1. Obtener dimensiones
            double canvasW = DesignGrid.Width;  // 2000
            double canvasH = DesignGrid.Height; // 2000

            // Usamos ActualWidth si está disponible, sino Width
            double formW = WindowResizerGrid.ActualWidth > 0 ? WindowResizerGrid.ActualWidth : WindowResizerGrid.Width;
            double formH = WindowResizerGrid.ActualHeight > 0 ? WindowResizerGrid.ActualHeight : WindowResizerGrid.Height;

            // Si por alguna razón siguen siendo NaN (no renderizado), forzamos valores default
            if (double.IsNaN(formW)) formW = 600;
            if (double.IsNaN(formH)) formH = 450;

            // 2. Calcular Márgenes para empujar el formulario al centro del lienzo
            double left = (canvasW - formW) / 2;
            double top = (canvasH - formH) / 2;

            // Aplicar la posición física al Grid del Formulario
            WindowResizerGrid.Margin = new Thickness(Math.Max(0, left), Math.Max(0, top), 0, 0);

            // 3. Mover la Cámara (ScrollViewer) para enfocar ese punto
            // Forzamos update para que el ScrollViewer sepa el tamaño real antes de moverse
            this.UpdateLayout();

            if (MainScrollViewer.ViewportWidth > 0 && MainScrollViewer.ViewportHeight > 0)
            {
                // El centro del formulario + el margen izquierdo - la mitad de la vista
                double scrollH = (left + (formW / 2)) - (MainScrollViewer.ViewportWidth / 2);
                double scrollV = (top + (formH / 2)) - (MainScrollViewer.ViewportHeight / 2);

                MainScrollViewer.ScrollToHorizontalOffset(scrollH);
                MainScrollViewer.ScrollToVerticalOffset(scrollV);
            }
        }

        // ============================
        // CONTROL DE ESTADO (DIRTY)
        // ============================

        

        private bool _isDirty;
        public bool IsDirty
        {
            get { return _currentVersionId != _savedVersionId; }
        }



        public void MarkAsClean()
        {
            _currentVersionId = Guid.NewGuid(); // Generamos identidad inicial
            _savedVersionId = _currentVersionId; // Decimos "Esto es lo guardado"
            CheckDirtyStatus(); // Debería dar Clean
        }

        private void CheckDirtyStatus()
        {
            // Notificamos siempre, la ventana principal decidirá poner o quitar el *
            IsDirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        private void GenerateNewVersion()
        {
            _currentVersionId = Guid.NewGuid();
            CheckDirtyStatus();
        }

        public void ClearCanvas()
        {
            // 1. Limpiar hijos visuales
            DesignSurface.Children.Clear();

            // 2. Limpiar variables de la clase parcial Interaction (Multiselección)
            _resizeHandles.Clear();
            _selectionAdorners.Clear(); // CORREGIDO: Antes era _selectionBorder
            _selectedControls.Clear();  // Importante limpiar la selección lógica también

            // 3. Notificar que no hay nada seleccionado
            ControlSelected?.Invoke(this, null);

            if (SelectionRect != null)
            {
                // Lo agregamos de nuevo
                DesignSurface.Children.Add(SelectionRect);

                // (Opcional) Aseguramos que se dibuje por encima de todo lo demás
                Panel.SetZIndex(SelectionRect, int.MaxValue);
            }
            if (QuickEditBox != null)
            {
                DesignSurface.Children.Add(QuickEditBox);
                Panel.SetZIndex(QuickEditBox, int.MaxValue); // Máximo nivel (siempre arriba)
                QuickEditBox.Visibility = Visibility.Collapsed; // Aseguramos que empiece oculta
            }
        }

        // ============================
        // LÓGICA DE CARGA (LoadForm)
        // ============================
        public void LoadForm(string vb6Content)
        {
            ClearCanvas();
            var rootModel = Vb6Helpers.ParseVb6Form(vb6Content);
            if (rootModel == null) return;

            // Dimensiones
            double fWidth = 600, fHeight = 450;
            double chromeW = 8, chromeH = 28;

            if (rootModel.Properties.ContainsKey("ClientWidth")) fWidth = Vb6Helpers.TwipsToPixels(rootModel.Properties["ClientWidth"]) + chromeW;
            else if (rootModel.Properties.ContainsKey("ScaleWidth")) fWidth = Vb6Helpers.TwipsToPixels(rootModel.Properties["ScaleWidth"]) + chromeW;

            if (rootModel.Properties.ContainsKey("ClientHeight")) fHeight = Vb6Helpers.TwipsToPixels(rootModel.Properties["ClientHeight"]) + chromeH;
            else if (rootModel.Properties.ContainsKey("ScaleHeight")) fHeight = Vb6Helpers.TwipsToPixels(rootModel.Properties["ScaleHeight"]) + chromeH;

            SetFormDimensions(fWidth, fHeight); // Definido en DesignerCanvas.FormResizing.cs

            if (rootModel.Properties.ContainsKey("Caption")) FormTitle = rootModel.Properties["Caption"];

            RenderChildren(rootModel, DesignSurface);

            CenterFormOnCanvas();

            MarkAsClean();
            SaveUndoSnapshot();
        }

        private void RenderChildren(VbControlModel model, FrameworkElement container)
        {
            Panel targetPanel = null;
            if (container is Panel p) targetPanel = p;
            else if (container is Border b && b.Child is Panel cp) targetPanel = cp;
            else if (container is GroupBox g && g.Content is Panel gp) targetPanel = gp;

            if (targetPanel == null) return;

            foreach (var child in model.Children)
            {
                if (child.Type.Contains("Menu")) continue;
                string type = child.Type.Contains(".") ? child.Type.Split('.')[1] : child.Type;

                // Mapeo rápido de nombres raros
                if (type.Contains("ucBtnSkin") || type.Contains("Toolbar")) type = "CommandButton";
                if (type.Contains("ListView")) type = "ListBox";
                if (type.Contains("ImageList")) type = "Timer";

                // Usamos la Factory externa
                UIElement element = RetroControlFactory.Create(type);

                if (element != null)
                {
                    var fe = element as FrameworkElement;

                    // Conectar eventos de interacción (Definidos en Interaction.cs)
                    fe.PreviewMouseDown += Control_PreviewMouseDown;
                    fe.PreviewMouseMove += Control_PreviewMouseMove;
                    fe.PreviewMouseUp += Control_PreviewMouseUp;
                    fe.Cursor = Cursors.SizeAll;

                    // Posición y Tamaño
                    double l = Vb6Helpers.TwipsToPixels(Vb6Helpers.GetPropVal(child, "Left"));
                    double t = Vb6Helpers.TwipsToPixels(Vb6Helpers.GetPropVal(child, "Top"));
                    double w = Vb6Helpers.TwipsToPixels(Vb6Helpers.GetPropVal(child, "Width"));
                    double h = Vb6Helpers.TwipsToPixels(Vb6Helpers.GetPropVal(child, "Height"));

                    if (w > 0) fe.Width = w;
                    if (h > 0) fe.Height = h;

                    // Textos
                    if (element is ContentControl cc && child.Properties.ContainsKey("Caption")) cc.Content = child.Properties["Caption"];
                    if (element is TextBox tb && child.Properties.ContainsKey("Text")) tb.Text = child.Properties["Text"];

                    // IMPORTANTE: Si es un array, guardamos esa info en el Tag, 
                    // pero intentamos no perder el Tipo original para el Undo/Redo
                    if (child.Properties.ContainsKey("Index"))
                    {
                        // Guardamos un formato compuesto si es necesario, o priorizamos el index visualmente
                        fe.Tag = "Array: " + child.Properties["Index"];
                    }

                    targetPanel.Children.Add(element);
                    Canvas.SetLeft(element, l);
                    Canvas.SetTop(element, t);

                    // Recursividad
                    if (child.Children.Count > 0) RenderChildren(child, fe);
                }
            }
        }

        public List<MenuModel> CurrentMenus { get; set; } = new List<MenuModel>();

        public void RenderMenus(List<MenuModel> menuItems)
        {
            this.CurrentMenus = menuItems;

            if (MenuArea == null) return;

            MenuArea.Children.Clear();

            // Si no hay menús, ocultamos el área para no robar espacio
            if (menuItems == null || !menuItems.Any())
            {
                MenuArea.Visibility = Visibility.Collapsed;
                return;
            }

            MenuArea.Visibility = Visibility.Visible;

            // Crear el control Menu principal
            Menu mainMenu = new Menu
            {
                Style = RetroStyles.GetVB6MenuStyle(),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8"))
            };

            // VB6 usa una lista plana con niveles. Primero convertimos o iteramos por nivel 0.
            // Asumiendo que MenuModel tiene 'Level' (0 = Top Level)
            var topLevelItems = menuItems.Where(m => m.Level == 0).ToList();

            foreach (var itemModel in topLevelItems)
            {
                mainMenu.Items.Add(CreateWpfMenuItem(itemModel, menuItems));
            }

            MenuArea.Children.Add(mainMenu);
        }

        private Control CreateWpfMenuItem(MenuModel model, List<MenuModel> allItems)
        {
            // Si el caption es "-", en VB6 es un separador
            if (model.Caption == "-")
            {
                return new Separator();
            }

            MenuItem wpfItem = new MenuItem
            {
                Header = model.Caption.Replace("&", "_"), // VB6 usa & para mnemónicos, WPF usa _
                IsEnabled = model.Enabled,
                // Usamos Visibility.Collapsed si model.Visible es false para que no ocupe espacio
                Visibility = model.Visible ? Visibility.Visible : Visibility.Collapsed
            };

            // Buscar hijos: son los elementos que siguen al actual en la lista y tienen Level > actual
            // hasta encontrar uno con Level <= actual.
            int currentIndex = allItems.IndexOf(model);
            for (int i = currentIndex + 1; i < allItems.Count; i++)
            {
                var possibleChild = allItems[i];
                if (possibleChild.Level <= model.Level) break; // Fin de la rama

                // Solo agregamos como hijo directo si es exactamente 1 nivel superior
                if (possibleChild.Level == model.Level + 1)
                {
                    wpfItem.Items.Add(CreateWpfMenuItem(possibleChild, allItems));
                }
            }

            return wpfItem;
        }


        public void ToggleRunMode()
        {
            IsRunMode = !IsRunMode;

            if (IsRunMode)
            {
                // === MODO RUN (PLAY) ===
                ClearSelection();

                // 1. Ocultar solo las herramientas de edición
                if (SelectionRect != null) SelectionRect.Visibility = Visibility.Collapsed;
                if (SnapLineOverlay != null) SnapLineOverlay.Visibility = Visibility.Collapsed;
                if (FormResizeHandles != null) FormResizeHandles.Visibility = Visibility.Collapsed;
                if (QuickEditBox != null) QuickEditBox.Visibility = Visibility.Collapsed;

                // 2. NO OCULTAR EL FONDO (Para evitar parpadeo gris/negro)
                // if (GridOverlay != null) GridOverlay.Visibility = Visibility.Collapsed; // <--- COMENTAR ESTO

                this.Cursor = Cursors.Arrow;
            }
            else
            {
                // === MODO DISEÑO (STOP) ===
                if (FormResizeHandles != null) FormResizeHandles.Visibility = Visibility.Visible;
            }
        }
    }


}
