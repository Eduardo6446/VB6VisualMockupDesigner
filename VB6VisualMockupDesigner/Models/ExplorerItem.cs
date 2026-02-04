using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace VB6VisualMockupDesigner.Models
{
    /// <summary>
    /// Defines the type of explorer item (Project, Folder, or File).
    /// </summary>
    public enum ExplorerItemType { Project, Folder, File }
    
    /// <summary>
    /// Represents an item in the project explorer tree.
    /// </summary>
    public class ExplorerItem : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the display name of the item.
        /// </summary>
        public string Name { get; set; }
        private string _fullPath;
        
        /// <summary>
        /// Gets or sets the full file path of the item.
        /// </summary>
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
        
        /// <summary>
        /// Gets or sets the type of explorer item.
        /// </summary>
        public ExplorerItemType Type { get; set; }
        
        /// <summary>
        /// Gets or sets the collection of child items.
        /// </summary>
        public ObservableCollection<ExplorerItem> Children { get; set; } = new ObservableCollection<ExplorerItem>();

        /// <summary>
        /// Gets the icon code based on the item type and file extension.
        /// </summary>
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
        
        /// <summary>
        /// Gets or sets whether the tree item is expanded.
        /// </summary>
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
        
        /// <summary>
        /// Gets or sets whether the tree item is selected.
        /// </summary>
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
