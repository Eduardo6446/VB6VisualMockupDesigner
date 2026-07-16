using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VB6VisualMockupDesigner.Models
{
    /// <summary>
    /// Represents a menu item in the VB6 menu editor.
    /// </summary>
    public class MenuModel : INotifyPropertyChanged
    {
        private string _caption;
        private string _name;
        private int _level;
        private bool _checked;
        private bool _enabled = true;
        private bool _visible = true;
        private string _shortcut;

        /// <summary>
        /// Gets the display caption with indentation based on menu level.
        /// </summary>
        // Propiedad que el ListBox mostrará (Ej: "....Archivo")
        public string DisplayCaption
        {
            get
            {
                // 4 puntos por nivel de indentación
                string indent = new string('.', _level * 4);
                return indent + _caption;
            }
        }

        /// <summary>
        /// Gets or sets the menu caption text.
        /// </summary>
        public string Caption
        {
            get => _caption;
            set { _caption = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayCaption)); }
        }

        /// <summary>
        /// Gets or sets the menu name identifier.
        /// </summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the menu nesting level (0 = top level).
        /// </summary>
        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayCaption)); }
        }

        /// <summary>
        /// Gets or sets whether the menu item is checked.
        /// </summary>
        public bool Checked
        {
            get => _checked;
            set { _checked = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets whether the menu item is enabled.
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets whether the menu item is visible.
        /// </summary>
        public bool Visible
        {
            get => _visible;
            set { _visible = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Gets or sets the keyboard shortcut for the menu item.
        /// </summary>
        public string Shortcut
        {
            get => _shortcut;
            set { _shortcut = value; OnPropertyChanged(); }
        }

        // Implementación básica de INotifyPropertyChanged para que la UI se actualice sola
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}