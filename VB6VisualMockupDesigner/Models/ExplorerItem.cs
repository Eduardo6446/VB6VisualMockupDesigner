using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace VB6VisualMockupDesigner.Models
{

    public enum ExplorerItemType { Project, Folder, File }
    public class ExplorerItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public ExplorerItemType Type { get; set; }
        public ObservableCollection<ExplorerItem> Children { get; set; } = new ObservableCollection<ExplorerItem>();

        public string IconCode
        {
            get
            {
                if (Type == ExplorerItemType.Project) return "\xE82D";
                if (Type == ExplorerItemType.Folder) return "\xE8B7";
                return "\xE8A5";
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
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
