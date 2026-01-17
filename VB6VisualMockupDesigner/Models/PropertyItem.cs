using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VB6VisualMockupDesigner.Models
{
    public enum PropertyType
    {
        Text,       // TextBox simple
        Number,     // Solo números
        Boolean,    // Dropdown True/False
        Enum,       // Dropdown con opciones personalizadas (ej: Alignment)
        Color,      // Texto hexadecimal + Previsualización de color
        ReadOnly,    // Solo lectura (ej: (Name) en ciertos casos)
        File,       // <--- NUEVO: Para imágenes (Picture, Icon)
        Font        // <--- NUEVO: Para fuentes (Font)
    }

    public class PropertyItem : INotifyPropertyChanged
    {
        private object _value;

        public string Name { get; set; }
        public string Category { get; set; } = "Misc";
        public PropertyType Type { get; set; } = PropertyType.Text;

        public string Description { get; set; } // <--- NUEVO CAMPO

        // Para Enums (ej: ["0 - Left", "1 - Right"])
        public List<string> Options { get; set; }

        public object Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}