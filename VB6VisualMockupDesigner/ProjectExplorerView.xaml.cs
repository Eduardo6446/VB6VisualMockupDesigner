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

        private bool _isNavigatingFromCode = false;

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

            if (_isNavigatingFromCode) return;


            if (ProjectTree.SelectedItem is ExplorerItem item && item.Type == ExplorerItemType.File)
            {
                OnFileOpened?.Invoke(this, item.FullPath);
            }
        }

        // Método SelectFile actualizado para trabajar con TreeView (Búsqueda recursiva simple)
        // --- NUEVA LÓGICA DE SINCRONIZACIÓN ---

        public void SelectFile(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || ProjectTree.ItemsSource == null) return;

            var items = ProjectTree.ItemsSource as IEnumerable<ExplorerItem>;
            if (items == null) return;

            _isNavigatingFromCode = true; // Bloqueamos eventos de UI

            foreach (var item in items)
            {
                // Limpiamos selecciones previas (opcional, pero recomendado)
                item.IsSelected = false;

                // Iniciamos búsqueda recursiva
                if (FindAndSelectRecursive(item, fullPath))
                {
                    break;
                }
            }

            _isNavigatingFromCode = false; // Liberamos eventos
        }

        private bool FindAndSelectRecursive(ExplorerItem currentItem, string targetPath)
        {
            // 1. ¿Es este el archivo?
            if (string.Equals(currentItem.FullPath, targetPath, System.StringComparison.OrdinalIgnoreCase))
            {
                currentItem.IsSelected = true; // Esto activará el trigger visual en XAML
                return true; // Encontrado
            }

            // 2. Buscamos en los hijos
            foreach (var child in currentItem.Children)
            {
                if (FindAndSelectRecursive(child, targetPath))
                {
                    // 3. ¡IMPORTANTE! Si encontramos al hijo, expandimos al padre
                    // Esto asegura que la carpeta se abra visualmente
                    currentItem.IsExpanded = true;
                    return true; // Retornamos éxito hacia arriba
                }
            }

            return false; // No estaba en esta rama
        }


        public List<ExplorerItem> SearchFiles(string query)
        {
            var results = new List<ExplorerItem>();
            if (string.IsNullOrWhiteSpace(query) || ProjectTree.ItemsSource == null)
                return results;

            var items = ProjectTree.ItemsSource as IEnumerable<ExplorerItem>;
            if (items == null) return results;

            foreach (var item in items)
            {
                SearchRecursive(item, query, results);
            }

            return results;
        }

        private void SearchRecursive(ExplorerItem item, string query, List<ExplorerItem> results)
        {
            // Solo buscamos archivos, no carpetas (a menos que quieras buscar carpetas también)
            if (item.Type == ExplorerItemType.File)
            {
                if (item.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    results.Add(item);
                }
            }

            // Buscar en hijos
            foreach (var child in item.Children)
            {
                SearchRecursive(child, query, results);
            }
        }
    }
}
