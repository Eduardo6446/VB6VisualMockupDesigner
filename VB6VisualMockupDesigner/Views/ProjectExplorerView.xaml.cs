using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Helpers;
using VB6VisualMockupDesigner.Models; // Requiere .NET Core 3.1 o superior (o NuGet en .NET Framework)
using VB6VisualMockupDesigner.Services;

namespace VB6VisualMockupDesigner.Views
{
    public partial class ProjectExplorerView : UserControl
    {
        public event System.EventHandler<string> OnFileOpened;

        private bool _isNavigatingFromCode = false;

        public ProjectExplorerView()
        {
            InitializeComponent();
        }

        // Método principal para cargar un proyecto
        public void LoadProjectStructure(string vbpPath)
        {
            try
            {
                // 1. Obtener el directorio raíz del proyecto
                string projectDir = System.IO.Path.GetDirectoryName(vbpPath);
                string projectName = System.IO.Path.GetFileNameWithoutExtension(vbpPath);

                // 2. Crear el nodo raíz visual
                var rootItem = new ExplorerItem
                {
                    Name = projectName,
                    FullPath = vbpPath,
                    Type = ExplorerItemType.Project,
                    IsExpanded = true, // Expandir por defecto
                    Children = new ObservableCollection<ExplorerItem>()
                };

                // 3. Escanear recursivamente el disco real (Estilo VS Code)
                BuildFileSystemTree(projectDir, rootItem.Children);

                // 4. Asignar al árbol
                ProjectTree.ItemsSource = new ObservableCollection<ExplorerItem> { rootItem };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la estructura del proyecto: " + ex.Message);
            }
        }

        // NUEVO MÉTODO AUXILIAR: Escanea carpetas y archivos reales
        private void BuildFileSystemTree(string dirPath, ObservableCollection<ExplorerItem> collection)
        {
            var dirInfo = new System.IO.DirectoryInfo(dirPath);

            // A. Procesar Carpetas
            foreach (var dir in dirInfo.GetDirectories())
            {
                // Ignorar carpetas ocultas o de sistema (como .git, .vs, bin, obj)
                if (dir.Name.StartsWith(".") || dir.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) || dir.Name.Equals("obj", StringComparison.OrdinalIgnoreCase))
                    continue;

                var folderItem = new ExplorerItem
                {
                    Name = dir.Name,
                    FullPath = dir.FullName,
                    Type = ExplorerItemType.Folder,
                    Children = new ObservableCollection<ExplorerItem>()
                };

                // Recursividad: Buscar dentro de esta carpeta
                BuildFileSystemTree(dir.FullName, folderItem.Children);

                collection.Add(folderItem);
            }

            // B. Procesar Archivos
            foreach (var file in dirInfo.GetFiles())
            {
                string ext = file.Extension.ToLower();

                // FILTRO: Solo mostrar archivos relevantes para VB6 o el diseñador
                if (ext == ".frm" || ext == ".bas" || ext == ".cls" || ext == ".ctl" || ext == ".vbp")
                {
                    // Opcional: No duplicar el archivo .vbp si ya es el nodo raíz, o mostrarlo como archivo editable
                    if (ext == ".vbp") continue;

                    var fileItem = new ExplorerItem
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        Type = ExplorerItemType.File,
                        Children = new ObservableCollection<ExplorerItem>()
                    };
                    collection.Add(fileItem);
                }
            }
        }

        // --- EVENTOS DEL TOOLBAR ---

        private void BtnNewFile_Click(object sender, RoutedEventArgs e)
        {
            CreateNewItem(false); // false = Archivo
        }

        private void BtnNewFolder_Click(object sender, RoutedEventArgs e)
        {
            CreateNewItem(true); // true = Carpeta
        }

        private void BtnCollapseAll_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectTree.ItemsSource is ObservableCollection<ExplorerItem> items)
            {
                foreach (var item in items)
                {
                    CollapseRecursive(item);
                }
            }
        }

        private void CreateNewItem(bool isFolder)
        {
            // 1. Obtener directorio base según la selección
            var (targetDir, parentItem) = GetTargetDirectory();
            if (string.IsNullOrEmpty(targetDir)) return;

            // 2. Pedir nombre (Usamos un InputBox simple hecho en código)
            string defaultName = isFolder ? "NuevaCarpeta" : "Formulario1";
            string title = isFolder ? "Nueva Carpeta" : "Nuevo Mockup";
            string extension = isFolder ? "" : ".frm";

            string name = SimpleInputBox.Show(title, "Introduce el nombre:", defaultName);
            if (string.IsNullOrWhiteSpace(name)) return;

            // Asegurar extensión si es archivo
            if (!isFolder && !name.EndsWith(".frm", StringComparison.OrdinalIgnoreCase))
                name += ".frm";

            string fullPath = Path.Combine(targetDir, name);

            try
            {
                if (isFolder)
                {
                    if (Directory.Exists(fullPath)) throw new Exception("La carpeta ya existe.");
                    Directory.CreateDirectory(fullPath);
                }
                else
                {
                    if (File.Exists(fullPath)) throw new Exception("El archivo ya existe.");
                    // Crear un formulario vacío básico
                    string basicTemplate =
$@"VERSION 5.00
Begin VB.Form {Path.GetFileNameWithoutExtension(name)} 
   Caption         =   ""{Path.GetFileNameWithoutExtension(name)}""
   ClientHeight    =   3195
   ClientLeft      =   60
   ClientTop       =   345
   ClientWidth     =   4680
   ScaleHeight     =   3195
   ScaleWidth      =   4680
   StartUpPosition =   3  'Windows Default
End
Attribute VB_Name = ""{Path.GetFileNameWithoutExtension(name)}""
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
";
                    File.WriteAllText(fullPath, basicTemplate);
                }

                // 3. Actualizar el árbol visualmente
                var newItem = new ExplorerItem
                {
                    Name = name,
                    FullPath = fullPath,
                    Type = isFolder ? ExplorerItemType.Folder : ExplorerItemType.File,
                    //IconCode = isFolder ? "\xE8B7" : "\xE7C3", // Iconos Folder / File
                    Children = new ObservableCollection<ExplorerItem>()
                };

                // Si hay un item padre seleccionado, añadimos ahí. Si no, al root.
                if (parentItem != null)
                {
                    parentItem.Children.Add(newItem);
                    parentItem.IsExpanded = true;
                }
                else if (ProjectTree.ItemsSource is ObservableCollection<ExplorerItem> roots && roots.Count > 0)
                {
                    // Añadimos a la raíz del proyecto (usualmente el primer nodo es el proyecto)
                    roots[0].Children.Add(newItem);
                    roots[0].IsExpanded = true;
                }

                // 4. Si es archivo, abrirlo automáticamente
                if (!isFolder)
                {
                    SelectFile(fullPath);
                    OnFileOpened?.Invoke(this, fullPath);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper para saber dónde crear el archivo
        private (string path, ExplorerItem item) GetTargetDirectory()
        {
            var selected = ProjectTree.SelectedItem as ExplorerItem;

            // Si no hay nada seleccionado, intentamos usar la raíz
            if (selected == null)
            {
                var roots = ProjectTree.ItemsSource as ObservableCollection<ExplorerItem>;
                if (roots != null && roots.Count > 0)
                {
                    string rootPath = Path.GetDirectoryName(roots[0].FullPath);
                    return (rootPath, roots[0]); // Retornamos el nodo raíz como padre
                }
                return (null, null);
            }

            // Si es carpeta, devolvemos su ruta
            if (selected.Type == ExplorerItemType.Folder || selected.Type == ExplorerItemType.Project)
            {
                return (selected.FullPath, selected);
            }
            // Si es archivo, devolvemos la ruta de su padre
            else
            {
                // OJO: Aquí hay un truco. ProjectTree.SelectedItem no nos da el padre.
                // Usamos Path.GetDirectoryName del archivo seleccionado.
                // Pero para actualizar la UI necesitamos el objeto ExplorerItem del padre.
                // Como buscar el padre en el árbol es costoso, para simplificar añadiremos al mismo nivel visualmente
                // si implementamos una búsqueda inversa, pero por ahora lo añadiremos a la raíz visual o
                // implementamos una búsqueda rápida del padre.

                // Opción Rápida: Devolver ruta del archivo padre, pero insertamos en la raíz o buscamos el padre recursivamente.
                string dir = Path.GetDirectoryName(selected.FullPath);
                var parent = FindParentItem(ProjectTree.ItemsSource as IEnumerable<ExplorerItem>, selected);
                return (dir, parent);
            }
        }


        private ExplorerItem FindParentItem(IEnumerable<ExplorerItem> items, ExplorerItem target)
        {
            if (items == null) return null;
            foreach (var item in items)
            {
                if (item.Children.Contains(target)) return item;
                var found = FindParentItem(item.Children, target);
                if (found != null) return found;
            }
            return null;
        }

        private void CollapseRecursive(ExplorerItem item)
        {
            item.IsExpanded = false;
            foreach (var child in item.Children)
            {
                CollapseRecursive(child);
            }
        }











        // Evento cuando seleccionamos algo en el árbol
        private void ProjectTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

            if (_isNavigatingFromCode) return;


            //if (ProjectTree.SelectedItem is ExplorerItem item && item.Type == ExplorerItemType.File)
            //{
            //    OnFileOpened?.Invoke(this, item.FullPath);
            //}
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

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }

        // Helper para encontrar el TreeViewItem visualmente
        static TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        // ---------------- ACCIONES DEL MENÚ ----------------

        // ABRIR
        private void MnuOpen_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectTree.SelectedItem is ExplorerItem item && item.Type == ExplorerItemType.File)
            {
                OnFileOpened?.Invoke(this, item.FullPath);
            }
        }

        // NUEVO ARCHIVO (Contextual)
        private void MnuNewFileContext_Click(object sender, RoutedEventArgs e)
        {
            // Reutilizamos tu lógica existente, que ya detecta la selección
            BtnNewFile_Click(sender, e);
        }

        // NUEVA CARPETA (Contextual)
        private void MnuNewFolderContext_Click(object sender, RoutedEventArgs e)
        {
            BtnNewFolder_Click(sender, e);
        }

        // RENOMBRAR
        private void MnuRename_Click(object sender, RoutedEventArgs e)
        {
            if (!(ProjectTree.SelectedItem is ExplorerItem item)) return;

            string newName = SimpleInputBox.Show("Renombrar", "Nuevo nombre:", item.Name);

            if (string.IsNullOrWhiteSpace(newName) || newName == item.Name) return;

            string parentDir = System.IO.Path.GetDirectoryName(item.FullPath);
            string newPath = System.IO.Path.Combine(parentDir, newName);

            try
            {
                if (item.Type == ExplorerItemType.Folder)
                {
                    System.IO.Directory.Move(item.FullPath, newPath);
                }
                else
                {
                    // Mantener extensión si el usuario la borró
                    if (!newName.EndsWith(".frm", StringComparison.OrdinalIgnoreCase) && item.FullPath.EndsWith(".frm"))
                        newPath += ".frm";

                    System.IO.File.Move(item.FullPath, newPath);
                }

                // Actualizar modelo
                item.Name = System.IO.Path.GetFileName(newPath);
                item.FullPath = newPath;

                // NOTA: Si quisieras ser perfecto, deberías actualizar recursivamente los FullPath de los hijos si es carpeta.
                // Por ahora, para un archivo simple, esto basta.
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al renombrar: " + ex.Message);
            }
        }

        // ELIMINAR
        private void MnuDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!(ProjectTree.SelectedItem is ExplorerItem item)) return;

            var res = MessageBox.Show($"¿Estás seguro de eliminar '{item.Name}'?\nEsta acción no se puede deshacer.",
                                      "Eliminar", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    if (item.Type == ExplorerItemType.Folder)
                        System.IO.Directory.Delete(item.FullPath, true); // True = recursivo
                    else
                        System.IO.File.Delete(item.FullPath);

                    // Eliminar visualmente del árbol
                    // Truco: Necesitamos encontrar al padre visual para removerlo de su colección Children.
                    // Como no tenemos referencia directa al padre en ExplorerItem, 
                    // la forma más rápida es recargar o buscar.
                    // Para este ejemplo rápido: RECOMENDACIÓN -> Añadir propiedad "Parent" a ExplorerItem en el futuro.

                    // Opción Rápida: Recargar todo (Bruto pero seguro)
                    // LoadProjectStructure(...ruta del vbp original...); 

                    // Opción Elegante (Requiere buscar el padre en la colección):
                    DeleteFromTree(ProjectTree.ItemsSource as System.Collections.IList, item);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al eliminar: " + ex.Message);
                }
            }
        }

        private bool DeleteFromTree(System.Collections.IList items, ExplorerItem target)
        {
            if (items == null) return false;
            if (items.Contains(target))
            {
                items.Remove(target);
                return true;
            }

            foreach (ExplorerItem child in items)
            {
                if (DeleteFromTree(child.Children, target)) return true;
            }
            return false;
        }

        // REVELAR EN EXPLORADOR
        private void MnuReveal_Click(object sender, RoutedEventArgs e)
        {
            if (ProjectTree.SelectedItem is ExplorerItem item)
            {
                string argument = "/select, \"" + item.FullPath + "\"";
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
        }

        private void ProjectTree_DragOver(object sender, DragEventArgs e)
        {
            // 1. Validar que sea un archivo
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            // 2. Detectar sobre qué item estamos "flotando"
            TreeViewItem item = GetTreeViewItemUnderMouse(e.GetPosition(ProjectTree));

            // 3. Feedback visual: Seleccionar el item temporalmente para que el usuario sepa dónde caerá
            if (item != null)
            {
                // --- CORRECCIÓN AQUÍ ---
                // Activamos la bandera para que el evento SelectedItemChanged NO abra el archivo
                _isNavigatingFromCode = true;

                item.IsSelected = true;

                // Desactivamos la bandera inmediatamente
                _isNavigatingFromCode = false;
                // -----------------------

                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None; // Si no está sobre ningún nodo, no permitir (o permitir en raíz)
            }

            e.Handled = true;
        }

        private void ProjectTree_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            // 1. Obtener archivos arrastrados
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // 2. Obtener el destino
            // Buscamos el item bajo el mouse. Si no hay ninguno, asumimos la raíz del proyecto.
            TreeViewItem targetItem = GetTreeViewItemUnderMouse(e.GetPosition(ProjectTree));
            ExplorerItem targetData = null;
            string targetPath = "";

            if (targetItem != null)
            {
                targetData = targetItem.DataContext as ExplorerItem;
            }
            else
            {
                // Si soltó en el espacio vacío, usar la raíz (si existe)
                var roots = ProjectTree.ItemsSource as ObservableCollection<ExplorerItem>;
                if (roots != null && roots.Count > 0) targetData = roots[0];
            }

            if (targetData == null) return;

            // 3. Determinar la carpeta física destino
            if (targetData.Type == ExplorerItemType.File)
            {
                // Si soltó sobre un archivo, guardamos en la carpeta que contiene ese archivo
                targetPath = Path.GetDirectoryName(targetData.FullPath);
                // Para actualizar la UI, necesitamos el PADRE de este archivo.
                // (El truco rápido visual que usaremos abajo dependerá de si encontramos al padre)
            }
            else
            {
                // Si es carpeta o proyecto, esa es la ruta
                targetPath = targetData.FullPath;
                if (targetData.Type == ExplorerItemType.Project)
                    targetPath = Path.GetDirectoryName(targetData.FullPath); // Si es el .vbp, usar su carpeta
            }

            // 4. Copiar archivos
            foreach (string fileSource in files)
            {
                try
                {
                    string fileName = Path.GetFileName(fileSource);
                    string destFile = Path.Combine(targetPath, fileName);

                    // Evitar sobrescribir si es el mismo archivo
                    if (fileSource.Equals(destFile, StringComparison.OrdinalIgnoreCase)) continue;

                    // Si ya existe, preguntar o renombrar. Aquí preguntamos.
                    if (File.Exists(destFile))
                    {
                        var res = MessageBox.Show($"El archivo '{fileName}' ya existe en la carpeta destino.\n¿Deseas sobrescribirlo?",
                            "Confirmar reemplazo", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (res == MessageBoxResult.No) continue;
                    }

                    File.Copy(fileSource, destFile, true);

                    // 5. Actualizar la UI (Árbol)
                    // La forma más fácil y segura es refrescar el nodo destino si es una carpeta
                    if (targetData.Type == ExplorerItemType.Folder || targetData.Type == ExplorerItemType.Project)
                    {
                        // Opción A: Añadir manualmente a la colección Children
                        var newItem = new ExplorerItem
                        {
                            Name = fileName,
                            FullPath = destFile,
                            Type = IsDirectory(destFile) ? ExplorerItemType.Folder : ExplorerItemType.File,
                            Children = new ObservableCollection<ExplorerItem>()
                        };

                        // Si es carpeta, habría que escanearla, pero asumamos archivo simple por ahora
                        targetData.Children.Add(newItem);
                        targetData.IsExpanded = true;
                    }
                    else
                    {
                        // Si soltamos sobre un archivo, visualmente es difícil encontrar al padre para añadirlo a su colección.
                        // Una solución rápida es recargar todo el árbol (seguro pero menos eficiente)
                        // O implementar la búsqueda del padre que mencionamos antes.

                        // Por ahora, recarguemos para asegurar consistencia si soltamos sobre archivo
                        if (ProjectTree.ItemsSource is ObservableCollection<ExplorerItem> roots)
                            LoadProjectStructure(roots[0].FullPath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al copiar '{Path.GetFileName(fileSource)}': {ex.Message}");
                }
            }
        }

        // Helper auxiliar para saber si es carpeta
        private bool IsDirectory(string path)
        {
            try
            {
                return (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
            }
            catch { return false; }
        }

        // --- MAGIA VISUAL: ENCONTRAR EL NODO BAJO EL MOUSE ---
        private TreeViewItem GetTreeViewItemUnderMouse(Point position)
        {
            HitTestResult result = VisualTreeHelper.HitTest(ProjectTree, position);
            if (result == null) return null;

            DependencyObject dependencyObject = result.VisualHit;
            while (dependencyObject != null && !(dependencyObject is TreeViewItem))
            {
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            return dependencyObject as TreeViewItem;
        }



        // -----------------------------------------------------------
        // MEJORAS DE UX: TECLADO Y RATÓN
        // -----------------------------------------------------------

        private void ProjectTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Verificar que el doble clic fue sobre un item y no en el espacio vacío
            var item = GetTreeViewItemUnderMouse(e.GetPosition(ProjectTree));
            if (item != null && item.DataContext is ExplorerItem explorerItem)
            {
                // Solo abrimos si es archivo. Las carpetas ya se expanden/colapsan nativamente.
                if (explorerItem.Type == ExplorerItemType.File)
                {
                    OnFileOpened?.Invoke(this, explorerItem.FullPath);
                    e.Handled = true;
                }
            }
        }

        private void ProjectTree_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(ProjectTree.SelectedItem is ExplorerItem selectedItem)) return;

            // ENTER: Abrir archivo
            if (e.Key == Key.Enter)
            {
                if (selectedItem.Type == ExplorerItemType.File)
                {
                    OnFileOpened?.Invoke(this, selectedItem.FullPath);
                    e.Handled = true;
                }
            }
            // F2: Renombrar
            else if (e.Key == Key.F2)
            {
                // Reutilizamos la lógica del menú contextual
                MnuRename_Click(sender, e);
                e.Handled = true;
            }
            // DELETE: Eliminar
            else if (e.Key == Key.Delete)
            {
                // Reutilizamos la lógica del menú contextual
                MnuDelete_Click(sender, e);
                e.Handled = true;
            }
        }

        // Dentro de ProjectExplorerView.xaml.cs

        public void LoadFolderContents(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath)) return;

                // Limpiar para evitar duplicados si ya había algo cargado
                ProjectTree.ItemsSource = null;

                string folderName = System.IO.Path.GetFileName(folderPath);
                if (string.IsNullOrEmpty(folderName)) folderName = folderPath;

                var rootItem = new ExplorerItem
                {
                    Name = folderName,
                    FullPath = folderPath,
                    Type = ExplorerItemType.Folder,
                    IsExpanded = true,
                    Children = new ObservableCollection<ExplorerItem>()
                };

                // Escaneamos el disco físicamente
                BuildFileSystemTree(folderPath, rootItem.Children);

                ProjectTree.ItemsSource = new ObservableCollection<ExplorerItem> { rootItem };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la carpeta: " + ex.Message);
            }
        }

        public void Clear()
        {
            ProjectTree.ItemsSource = null;
        }

    }

    public static class SimpleInputBox
    {
        public static string Show(string title, string prompt, string defaultText = "")
        {
            Window win = new Window()
            {
                Width = 300,
                Height = 150,
                Title = title,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = (System.Windows.Media.Brush)Application.Current.Resources["PanelBackground"] ?? System.Windows.Media.Brushes.WhiteSmoke
            };

            StackPanel sp = new StackPanel() { Margin = new Thickness(10) };
            sp.Children.Add(new TextBlock() { Text = prompt, Margin = new Thickness(0, 0, 0, 5), Foreground = System.Windows.Media.Brushes.Black }); // Ajustar color si usas tema oscuro

            TextBox tb = new TextBox() { Text = defaultText };
            sp.Children.Add(tb);

            StackPanel btnPanel = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            Button btnOk = new Button() { Content = "Aceptar", Width = 70, IsDefault = true, Margin = new Thickness(0, 0, 5, 0) };
            Button btnCancel = new Button() { Content = "Cancelar", Width = 70, IsCancel = true };

            btnOk.Click += (s, e) => { win.DialogResult = true; win.Close(); };
            btnPanel.Children.Add(btnOk);
            btnPanel.Children.Add(btnCancel);
            sp.Children.Add(btnPanel);

            win.Content = sp;
            tb.SelectAll();
            tb.Focus();

            if (win.ShowDialog() == true)
                return tb.Text;

            return null;
        }






    }




}
