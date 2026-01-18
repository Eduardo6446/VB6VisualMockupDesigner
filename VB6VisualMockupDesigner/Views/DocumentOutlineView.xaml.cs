using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Models;

namespace VB6VisualMockupDesigner.Views
{
    public partial class DocumentOutlineView : UserControl
    {
        public event EventHandler CloseRequested;
        public event Action<FrameworkElement> NodeSelected; // Avisar al Main para seleccionar en Canvas

        private DesignerCanvas _currentDesigner;
        private bool _ignoreSelectionChange = false;

        public DocumentOutlineView()
        {
            InitializeComponent();
        }

        // ==========================================
        // CARGA DEL ÁRBOL
        // ==========================================
        public void LoadHierarchy(DesignerCanvas designer)
        {
            _currentDesigner = designer;
            if (designer == null)
            {
                OutlineTree.ItemsSource = null;
                return;
            }

            // Crear nodo raíz (El Formulario/Canvas)
            var rootNode = new DocumentOutlineNode
            {
                Name = designer.FormTitle ?? "Form1",
                Type = "Form",
                IconCode = "\xE700", // Icono App/Window
                ControlReference = null // La raíz no es un control seleccionable igual que los hijos
            };

            // Llenar recursivamente
            BuildTreeRecursive(designer.GetDesignSurface(), rootNode);

            // Asignar al árbol (WPF requiere una lista, aunque sea de 1 elemento raíz)
            OutlineTree.ItemsSource = new ObservableCollection<DocumentOutlineNode> { rootNode };
        }

        private void BuildTreeRecursive(Panel container, DocumentOutlineNode parentNode)
        {
            foreach (UIElement child in container.Children)
            {
                // Ignorar elementos de adorno (SelectionRect, Handles, etc)
                if (child is FrameworkElement fe && IsRealControl(fe))
                {
                    var node = new DocumentOutlineNode
                    {
                        Name = fe.Name,
                        Type = fe.GetType().Name.Replace("Box", "").Replace("Button", "Btn"),
                        ControlReference = fe,
                        IconCode = GetIconForControl(fe)
                    };

                    parentNode.Children.Add(node);

                    // RECURSIVIDAD: Si este control es un contenedor (Frame, PictureBox)
                    // tenemos que buscar dentro de él.
                    if (fe is GroupBox gb && gb.Content is Panel innerCanvas)
                    {
                        BuildTreeRecursive(innerCanvas, node);
                    }
                    else if (fe is Border bd && bd.Child is Panel innerPanel)
                    {
                        BuildTreeRecursive(innerPanel, node);
                    }
                }
            }
        }

        // Filtro para no mostrar basura visual
        private bool IsRealControl(FrameworkElement fe)
        {
            if (fe.Name == "SelectionRect") return false;
            if (fe.Name == "QuickEditBox") return false;
            if (fe is System.Windows.Shapes.Rectangle) return false; // Handles
            if (fe is Border && fe.Name == "") return false; // Bordes azules de selección
            return true;
        }

        private string GetIconForControl(FrameworkElement fe)
        {
            if (fe is Button) return "\xE816"; // Botón
            if (fe is TextBox) return "\xE8D2"; // Edit
            if (fe is TextBlock) return "\xE8D3"; // Label
            if (fe is GroupBox) return "\xE71D"; // Frame (Square)
            if (fe is Image) return "\xEB9F"; // Imagen
            if (fe is CheckBox) return "\xE73A"; // Check
            if (fe is RadioButton) return "\xE915"; // Radio
            return "\xE74C"; // Default (Circle)
        }

        // ==========================================
        // EVENTOS
        // ==========================================

        private void OutlineTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_ignoreSelectionChange) return;

            if (OutlineTree.SelectedItem is DocumentOutlineNode node && node.ControlReference != null)
            {
                NodeSelected?.Invoke(node.ControlReference);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDesigner != null) LoadHierarchy(_currentDesigner);
        }
    }
}