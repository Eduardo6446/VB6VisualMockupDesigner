using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VB6VisualMockupDesigner.Helpers;

namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Interaction logic for DesignerCanvas.xaml
    /// </summary>
    public partial class DesignerCanvas : UserControl
    {
        public DesignerCanvas()
        {
            InitializeComponent();
            this.Loaded += (s, e) => { CenterView(); };
        }

        public event EventHandler<FrameworkElement> ControlSelected;

        public string FormTitle
        {
            get { return FormTitleText.Text; }
            set { FormTitleText.Text = value; }
        }

        private void CenterView()
        {
            if (MainScrollViewer == null || DesignGrid == null || MainScrollViewer.ViewportWidth == 0) return;
            double hOff = (DesignGrid.Width / 2) - (MainScrollViewer.ViewportWidth / 2);
            double vOff = (DesignGrid.Height / 2) - (MainScrollViewer.ViewportHeight / 2);
            MainScrollViewer.ScrollToHorizontalOffset(hOff);
            MainScrollViewer.ScrollToVerticalOffset(vOff);
        }

        public void ClearCanvas()
        {
            DesignSurface.Children.Clear();
            _resizeHandles.Clear(); // Variable definida en la clase parcial Interaction
            _selectionBorder = null;
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

            SetFormDimensions(fWidth, fHeight); // Definido en FormResizing.cs

            if (rootModel.Properties.ContainsKey("Caption")) FormTitle = rootModel.Properties["Caption"];

            RenderChildren(rootModel, DesignSurface);
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
                    if (child.Properties.ContainsKey("Index")) fe.Tag = "Array: " + child.Properties["Index"];

                    targetPanel.Children.Add(element);
                    Canvas.SetLeft(element, l);
                    Canvas.SetTop(element, t);

                    // Recursividad
                    if (child.Children.Count > 0) RenderChildren(child, fe);
                }
            }
        }
    }


}
