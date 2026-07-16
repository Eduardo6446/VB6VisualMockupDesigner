using System;
using System.Collections.Generic;
using System.Text;

namespace VB6VisualMockupDesigner.Models
{
    /// <summary>
    /// Represents a VB6 control in the designer hierarchy.
    /// </summary>
    public class VbControlModel
    {
        /// <summary>
        /// Gets or sets the control type (e.g., VB.CommandButton).
        /// </summary>
        public string Type { get; set; }        // Ej: VB.CommandButton
        
        /// <summary>
        /// Gets or sets the control name identifier (e.g., cmdAceptar).
        /// </summary>
        public string Name { get; set; }        // Ej: cmdAceptar

        /// <summary>
        /// Gets or sets the dictionary of control properties (Left, Top, Caption, etc.).
        /// </summary>
        // Diccionario para propiedades (Left, Top, Caption, etc.)
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the list of child controls.
        /// </summary>
        // Lista de hijos (Aquí vive la jerarquía)
        public List<VbControlModel> Children { get; set; } = new List<VbControlModel>();

        /// <summary>
        /// Gets or sets the list of menu items for the control.
        /// </summary>
        public List<MenuModel> Menus { get; set; } = new List<MenuModel>();
    }
}
