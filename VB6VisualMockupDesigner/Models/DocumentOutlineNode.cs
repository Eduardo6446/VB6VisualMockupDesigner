using System.Collections.ObjectModel;
using System.Windows;

namespace VB6VisualMockupDesigner.Models
{
    public class DocumentOutlineNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string IconCode { get; set; } // Para el icono (fuente Segoe MDL2)
        public FrameworkElement ControlReference { get; set; } // Enlace al control real

        // Colección recursiva para los hijos (Observable para que la UI se actualice)
        public ObservableCollection<DocumentOutlineNode> Children { get; set; }

        public DocumentOutlineNode()
        {
            Children = new ObservableCollection<DocumentOutlineNode>();
        }

        public string DisplayName => string.IsNullOrEmpty(Name) ? $"[{Type}]" : Name;
    }
}