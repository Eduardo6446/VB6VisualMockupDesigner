using System;
using System.Collections.Generic;
using System.Text;

namespace VB6VisualMockupDesigner.Models
{
    public class VbControlModel
    {
        public string Type { get; set; }        // Ej: VB.CommandButton
        public string Name { get; set; }        // Ej: cmdAceptar

        // Diccionario para propiedades (Left, Top, Caption, etc.)
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        // Lista de hijos (Aquí vive la jerarquía)
        public List<VbControlModel> Children { get; set; } = new List<VbControlModel>();
    }
}
