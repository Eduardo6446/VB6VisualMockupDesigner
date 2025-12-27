using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public event System.EventHandler<string> OnFileOpened;

        public ProjectExplorerView()
        {
            InitializeComponent();
        }

        // Método principal para cargar un proyecto VBP
        public void LoadProjectStructure(string vbpPath)
        {
            // El método ParseProject ahora maneja sus propias excepciones y muestra MessageBox
            var rootItem = VbpParser.ParseProject(vbpPath);

            // Solo actualizamos la UI si obtuvimos un resultado válido
            if (rootItem != null)
            {
                ProjectTree.ItemsSource = new ObservableCollection<ExplorerItem> { rootItem };
            }
        }

        // Evento cuando seleccionamos algo en el árbol
        private void ProjectTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (ProjectTree.SelectedItem is ExplorerItem item)
            {
                // Solo abrimos si es un ARCHIVO, no una carpeta o el proyecto
                if (item.Type == ExplorerItemType.File)
                {
                    OnFileOpened?.Invoke(this, item.FullPath);
                }
            }
        }

        // Método SelectFile actualizado para trabajar con TreeView (Búsqueda recursiva simple)
        public void SelectFile(string fileName)
        {
            // Nota: Seleccionar programáticamente un item profundo en un TreeView de WPF
            // es complejo porque los nodos no visualizados no existen en memoria UI.
            // Para esta versión v0.8, podemos omitir la selección automática profunda 
            // o implementar una búsqueda solo en el primer nivel si es crítico.
            // Por ahora, lo dejaremos simple para evitar crashes.
        }
    }
}
