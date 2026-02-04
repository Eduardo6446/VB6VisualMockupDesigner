using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace VB6VisualMockupDesigner.Models
{
    /// <summary>
    /// Defines the types of properties that can be displayed in the property grid.
    /// </summary>
    public enum PropertyType
    {
        /// <summary>Text input property.</summary>
        Text,       // TextBox simple
        /// <summary>Numeric input property.</summary>
        Number,     // Solo números
        /// <summary>Boolean dropdown property.</summary>
        Boolean,    // Dropdown True/False
        /// <summary>Enumeration dropdown property.</summary>
        Enum,       // Dropdown con opciones personalizadas (ej: Alignment)
        /// <summary>Color picker property.</summary>
        Color,      // Texto hexadecimal + Previsualización de color
        /// <summary>Read-only property.</summary>
        ReadOnly,    // Solo lectura (ej: (Name) en ciertos casos)
        /// <summary>File picker property.</summary>
        File,       // <--- NUEVO: Para imágenes (Picture, Icon)
        /// <summary>Font picker property.</summary>
        Font,        // <--- NUEVO: Para fuentes (Font)
        /// <summary>String list editor property.</summary>
        StringList
    }

    /// <summary>
    /// Represents a property item displayed in the property grid.
    /// </summary>
    public class PropertyItem : INotifyPropertyChanged
    {
        private object _value;

        /// <summary>
        /// Gets or sets the property name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the property category.
        /// </summary>
        public string Category { get; set; } = "Misc";
        
        /// <summary>
        /// Gets or sets the property type.
        /// </summary>
        public PropertyType Type { get; set; } = PropertyType.Text;

        /// <summary>
        /// Gets or sets the property description.
        /// </summary>
        public string Description { get; set; } // <--- NUEVO CAMPO

        /// <summary>
        /// Gets or sets the available options for enum properties.
        /// </summary>
        // Para Enums (ej: ["0 - Left", "1 - Right"])
        public List<string> Options { get; set; }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        public object Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Clase auxiliar para guardar los datos del ComboBox
    class ControlItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public FrameworkElement Control { get; set; }

        // Esto es lo que mostrará el ComboBox si no usas DisplayMemberPath
        public override string ToString() => $"{Name} ({Type})";
    }
}