using System.Collections.ObjectModel;
using System.Windows;

namespace VB6VisualMockupDesigner.Models
{
    /// <summary>
    /// Represents a node in the document outline tree view.
    /// </summary>
    public class DocumentOutlineNode
    {
        /// <summary>
        /// Gets or sets the node name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the control type.
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets the icon code for display (Segoe MDL2 font).
        /// </summary>
        public string IconCode { get; set; } // Para el icono (fuente Segoe MDL2)
        
        /// <summary>
        /// Gets or sets the reference to the actual control in the designer.
        /// </summary>
        public FrameworkElement ControlReference { get; set; } // Enlace al control real

        /// <summary>
        /// Gets or sets the collection of child nodes.
        /// </summary>
        // Colección recursiva para los hijos (Observable para que la UI se actualice)
        public ObservableCollection<DocumentOutlineNode> Children { get; set; }

        /// <summary>
        /// Initializes a new instance of the DocumentOutlineNode class.
        /// </summary>
        public DocumentOutlineNode()
        {
            Children = new ObservableCollection<DocumentOutlineNode>();
        }

        /// <summary>
        /// Gets the display name for the node.
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(Name) ? $"[{Type}]" : Name;
    }
}