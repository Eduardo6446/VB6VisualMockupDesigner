using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace VB6VisualMockupDesigner.Models
{
    public class ToolboxItem
    {
        public string Name { get; set; }
        public string Category { get; set; } // Ej: "VB.Runtime", "Threed32.ocx"
        public string Type { get; set; }     // Clave para la Factory
        public Geometry IconData { get; set; } // El dibujo vectorial
    }
}
