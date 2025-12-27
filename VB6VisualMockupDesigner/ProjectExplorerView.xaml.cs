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
        public event System.EventHandler<string> OnFileOpened;

        // Flag para evitar bucles infinitos de eventos (Seleccionar lista -> Abre Tab -> Selecciona lista...)
        private bool _isInternalChange = false;

        public ProjectExplorerView()
        {
            InitializeComponent();
            FileList.SelectionChanged += FileList_SelectionChanged;
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInternalChange) return;

            if (FileList.SelectedItem is ListBoxItem item)
            {
                // Disparamos el evento hacia la ventana principal
                OnFileOpened?.Invoke(this, item.Tag.ToString());
            }
        }

        // ==========================================
        // NUEVO MÉTODO: Seleccionar archivo por nombre
        // ==========================================
        public void SelectFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            _isInternalChange = true; // Pausamos eventos para no re-abrir la pestaña innecesariamente

            bool found = false;

            // 1. Buscar si ya existe en la lista
            foreach (var item in FileList.Items)
            {
                if (item is ListBoxItem lbItem && lbItem.Tag?.ToString() == fileName)
                {
                    FileList.SelectedItem = lbItem;
                    FileList.ScrollIntoView(lbItem); // Importante: Asegura que sea visible
                    found = true;
                    break;
                }
            }

            // 2. Si no existe (es un archivo nuevo que acabamos de "Abrir"), lo agregamos visualmente
            if (!found)
            {
                var newItem = CreateNewExplorerItem(fileName);
                FileList.Items.Add(newItem);

                // Lo seleccionamos
                FileList.SelectedItem = newItem;
                FileList.ScrollIntoView(newItem);
            }

            _isInternalChange = false; // Reactivamos eventos
        }

        // Helper para crear el item visualmente con el estilo VS Code
        private ListBoxItem CreateNewExplorerItem(string fileName)
        {
            var item = new ListBoxItem();
            item.Tag = fileName;

            // Construimos el StackPanel visual (Icono + Texto)
            var stack = new StackPanel { Orientation = Orientation.Horizontal };

            // Icono
            var icon = new TextBlock
            {
                Text = "\xE8A5", // Icono de documento
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                Margin = new System.Windows.Thickness(0, 0, 8, 0),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#005A9E")) // Azul marca
            };

            // Texto
            var text = new TextBlock { Text = fileName };

            stack.Children.Add(icon);
            stack.Children.Add(text);

            item.Content = stack;
            return item;
        }
    }
}
