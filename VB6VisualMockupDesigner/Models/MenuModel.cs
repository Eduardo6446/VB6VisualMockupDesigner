using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VB6VisualMockupDesigner.Models
{
    public class MenuModel : INotifyPropertyChanged
    {
        private string _caption;
        private string _name;
        private int _level;
        private bool _checked;
        private bool _enabled = true;
        private bool _visible = true;
        private string _shortcut;

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

        public string Caption
        {
            get => _caption;
            set { _caption = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayCaption)); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public int Level
        {
            get => _level;
            set { _level = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayCaption)); }
        }

        public bool Checked
        {
            get => _checked;
            set { _checked = value; OnPropertyChanged(); }
        }

        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(); }
        }

        public bool Visible
        {
            get => _visible;
            set { _visible = value; OnPropertyChanged(); }
        }

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