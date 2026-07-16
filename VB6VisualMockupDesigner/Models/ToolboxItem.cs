using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace VB6VisualMockupDesigner.Models
{
    /// <summary>
    /// Represents an item in the designer toolbox.
    /// </summary>
    public class ToolboxItem
    {
        /// <summary>
        /// Gets or sets the display name of the toolbox item.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the category (e.g., "VB.Runtime", "Threed32.ocx").
        /// </summary>
        public string Category { get; set; } // Ej: "VB.Runtime", "Threed32.ocx"
        
        /// <summary>
        /// Gets or sets the type key used by the factory.
        /// </summary>
        public string Type { get; set; }     // Clave para la Factory
        
        /// <summary>
        /// Gets or sets the vector icon data.
        /// </summary>
        public Geometry IconData { get; set; } // El dibujo vectorial
    }
}
