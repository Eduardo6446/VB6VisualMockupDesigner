using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Interaction logic for ProjectExplorerView.xaml
    /// </summary>
    public partial class ProjectExplorerView : UserControl
    {
        public event EventHandler<string> OnFileOpened;

        public ProjectExplorerView()
        {
            InitializeComponent();
            FileList.SelectionChanged += FileList_SelectionChanged;
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileList.SelectedItem is ListBoxItem item)
            {
                // Disparamos el evento hacia la ventana principal
                OnFileOpened?.Invoke(this, item.Tag.ToString());

                // Reseteamos selección para permitir re-click (opcional)
                FileList.SelectedIndex = -1;
            }
        }
    }
}
