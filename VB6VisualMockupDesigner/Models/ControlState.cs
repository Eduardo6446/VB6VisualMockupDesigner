using System.Collections.Generic;

namespace VB6VisualMockupDesigner.Models
{
    public class ControlState
    {
        // Identificadores básicos
        public string Name { get; set; }        // Ej: cmdAceptar
        public string Type { get; set; }        // Ej: CommandButton, Label

        // Geometría (Aquí guardamos lo que corregimos del Bug de tamaños)
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        // Propiedades dinámicas (Caption, Text, ForeColor, etc.)
        // Usamos un diccionario para no tener que crear una propiedad por cada cosa posible en VB6
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        // Orden de capas (Importante para lo que hablamos del Z-Index)
        public int ZIndex { get; set; }

        // Contenedor padre (si está dentro de un Frame o PictureBox)
        public string ContainerName { get; set; }
    }
}