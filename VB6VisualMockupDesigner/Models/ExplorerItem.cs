using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace VB6VisualMockupDesigner.Models
{

    public enum ExplorerItemType { Project, Folder, File }
    public class ExplorerItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        private string _fullPath;
        public string FullPath
        {
            get => _fullPath;
            set
            {
                if (_fullPath != value)
                {
                    _fullPath = value;
                    OnPropertyChanged(); // Avisa que FullPath cambió

                    // ¡EL TRUCO! Avisamos que IconCode también cambió (porque depende de la extensión)
                    OnPropertyChanged(nameof(IconCode));
                }
            }
        }
        public ExplorerItemType Type { get; set; }
        public ObservableCollection<ExplorerItem> Children { get; set; } = new ObservableCollection<ExplorerItem>();

        // En VB6VisualMockupDesigner.Models.ExplorerItem.cs

        public string IconCode
        {
            get
            {
                if (Type == ExplorerItemType.Project) return "\xE82D";
                if (Type == ExplorerItemType.Folder) return "\xE8B7";

                // Lógica automática basada en la extensión del FullPath
                string ext = System.IO.Path.GetExtension(FullPath)?.ToLower();
                switch (ext)
                {
                    case ".frm": return "\xE7C3"; // Form
                    case ".bas": return "\xE943"; // Module
                    case ".cls": return "\xE99A"; // Class
                    case ".ctl": return "\xE74C"; // UserControl
                    case ".res": return "\xEA86"; // Resource
                    case ".ico":
                    case ".bmp": return "\xEB9F"; // Image
                    default: return "\xE7C3";     // File genérico
                }
            }
        }

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged("IsExpanded");
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        // --- IMPLEMENTACIÓN DE LA INTERFAZ ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
