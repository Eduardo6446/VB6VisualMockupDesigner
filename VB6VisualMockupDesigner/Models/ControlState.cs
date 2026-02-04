using System.Collections.Generic;

namespace VB6VisualMockupDesigner.Models
{
    /// <summary>
    /// Represents the state of a control for serialization and undo/redo operations.
    /// </summary>
    public class ControlState
    {
        /// <summary>
        /// Gets or sets the control name identifier.
        /// </summary>
        // Identificadores básicos
        public string Name { get; set; }        // Ej: cmdAceptar
        
        /// <summary>
        /// Gets or sets the control type.
        /// </summary>
        public string Type { get; set; }        // Ej: CommandButton, Label

        /// <summary>
        /// Gets or sets the left position of the control.
        /// </summary>
        // Geometría (Aquí guardamos lo que corregimos del Bug de tamaños)
        public double Left { get; set; }
        
        /// <summary>
        /// Gets or sets the top position of the control.
        /// </summary>
        public double Top { get; set; }
        
        /// <summary>
        /// Gets or sets the width of the control.
        /// </summary>
        public double Width { get; set; }
        
        /// <summary>
        /// Gets or sets the height of the control.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Gets or sets the dynamic properties dictionary (Caption, Text, ForeColor, etc.).
        /// </summary>
        // Propiedades dinámicas (Caption, Text, ForeColor, etc.)
        // Usamos un diccionario para no tener que crear una propiedad por cada cosa posible en VB6
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the Z-Index for layer ordering.
        /// </summary>
        // Orden de capas (Importante para lo que hablamos del Z-Index)
        public int ZIndex { get; set; }

        /// <summary>
        /// Gets or sets the name of the parent container (if inside a Frame or PictureBox).
        /// </summary>
        // Contenedor padre (si está dentro de un Frame o PictureBox)
        public string ContainerName { get; set; }
    }
}