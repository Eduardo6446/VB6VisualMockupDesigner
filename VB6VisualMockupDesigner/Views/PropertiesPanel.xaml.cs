using System;
using System.Collections.Generic;
using System.ComponentModel; // Necesario para ICollectionView y SortDescription
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data; // Necesario para ListCollectionView
using VB6VisualMockupDesigner.Helpers;
using VB6VisualMockupDesigner.Models;

namespace VB6VisualMockupDesigner.Views
{
    public partial class PropertiesPanel : UserControl
    {
        // Eventos
        public event EventHandler CloseRequested;
        public event EventHandler PropertyChanged;
        public event EventHandler PropertyChanging;
        public Func<string, bool> CheckNameAvailability;

        // Evento para el Object Selector (Combo de arriba)
        public event Action<FrameworkElement> ObjectSelectedFromList;

        // Variables privadas
        private FrameworkElement _currentControl;
        private bool _isUpdating;
        private bool _ignoreComboEvents = false;

        // VARIABLE CLAVE PARA EL BUSCADOR
        private ICollectionView _view;

        public PropertiesPanel()
        {
            InitializeComponent();
        }

        // ============================================================
        // 1. LÓGICA PRINCIPAL: INSPECCIONAR UN OBJETO
        // ============================================================
        public void InspectObject(FrameworkElement control)
        {
            _currentControl = control;
            _isUpdating = true;

            if (control == null)
            {
                PropGrid.ItemsSource = null;
                ObjectSelector.SelectedItem = null;
                _isUpdating = false;
                return;
            }

            // 1. Obtener la lista de propiedades del Helper
            var props = PropertyManager.GetPropertiesFor(control);

            // 2. CREAR LA VISTA FILTRABLE (Aquí está la magia del buscador)
            _view = new ListCollectionView(props);

            // 3. Conectar el filtro
            _view.Filter = FilterProperties;

            // 4. Configurar ordenación y agrupación inicial
            ApplySortingAndGrouping();

            // 5. Asignar al DataGrid
            PropGrid.ItemsSource = _view;

            _isUpdating = false;
        }

        // ============================================================
        // 2. LÓGICA DEL BUSCADOR (FILTER)
        // ============================================================

        // Evento que salta cada vez que escribes una letra
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Pedimos a la vista que se actualice pasando el filtro de nuevo
            _view?.Refresh();
        }

        // Esta función decide si una propiedad se muestra o se oculta
        private bool FilterProperties(object obj)
        {
            // Si la caja está vacía, mostramos todo
            if (string.IsNullOrWhiteSpace(SearchBox.Text)) return true;

            var prop = obj as PropertyItem;
            if (prop == null) return false;

            // Buscamos si el nombre contiene el texto (ignorando mayúsculas/minúsculas)
            return prop.Name.IndexOf(SearchBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ============================================================
        // 3. LÓGICA DEL OBJECT SELECTOR (COMBO SUPERIOR)
        // ============================================================

        // Clase interna para el combo
        private class ControlItem
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public FrameworkElement Control { get; set; }
            public override string ToString() => Name;
        }

        public void UpdateObjectList(IEnumerable<FrameworkElement> controls, FrameworkElement selected)
        {
            _ignoreComboEvents = true;
            ObjectSelector.ItemsSource = null;

            var comboItems = controls.Select(c => new ControlItem
            {
                Name = string.IsNullOrEmpty(c.Name) ? "[Sin Nombre]" : c.Name,
                Type = c.GetType().Name.Replace("Box", "").Replace("Button", "Btn"),
                Control = c
            }).OrderBy(x => x.Name).ToList();

            ObjectSelector.ItemsSource = comboItems;
            ObjectSelector.DisplayMemberPath = "Name";

            if (selected != null)
            {
                var itemToSelect = comboItems.FirstOrDefault(x => x.Control == selected);
                ObjectSelector.SelectedItem = itemToSelect;
            }
            _ignoreComboEvents = false;
        }

        private void ObjectSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreComboEvents) return;

            if (ObjectSelector.SelectedItem is ControlItem item)
            {
                ObjectSelectedFromList?.Invoke(item.Control);
            }
        }

        // ============================================================
        // 4. LÓGICA DE ORDENACIÓN (CATEGORIZADO VS ALFABÉTICO)
        // ============================================================
        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Primitives.ToggleButton;
            if (btn == null) return;

            // Lógica de "Radio Button" manual
            if (btn == BtnCategorized) BtnAlphabetical.IsChecked = false;
            else BtnCategorized.IsChecked = false;

            // Re-aplicar orden
            if (_view != null)
            {
                ApplySortingAndGrouping();
                _view.Refresh();
            }
        }

        private void ApplySortingAndGrouping()
        {
            if (_view == null) return;

            // Limpiar orden previo
            _view.GroupDescriptions.Clear();
            _view.SortDescriptions.Clear();

            if (BtnCategorized.IsChecked == true)
            {
                // Agrupar por Categoría
                _view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                // Ordenar: Primero Categoría, luego Nombre
                _view.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
                _view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }
            else
            {
                // Solo orden alfabético simple
                _view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }
        }

        // ============================================================
        // 5. EDICIÓN Y CAMBIOS (COMMIT)
        // ============================================================
        private void PropGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.Item as PropertyItem;
                if (item != null)
                {
                    // Pequeño hack para que el binding se actualice antes de aplicar
                    // (A veces el TextBox no ha enviado el dato al source todavía)
                    if (e.EditingElement is TextBox tb) item.Value = tb.Text;

                    ApplyChange(item);
                }
            }
        }

        private void ApplyChange(PropertyItem item)
        {
            if (_currentControl == null || _isUpdating) return;

            // Validación especial para el Nombre
            if (item.Name == "(Name)")
            {
                string newName = item.Value?.ToString();
                if (CheckNameAvailability != null && !CheckNameAvailability(newName))
                {
                    // Revertir si el nombre no es válido o está duplicado
                    item.Value = _currentControl.Name;
                    return;
                }
            }

            PropertyChanging?.Invoke(this, EventArgs.Empty); // Para Undo/Redo
            PropertyManager.ApplyProperty(_currentControl, item);
            PropertyChanged?.Invoke(this, EventArgs.Empty);  // Para actualizar visuales (bordes azules)
        }

        // ============================================================
        // 6. EVENTOS DE UI AUXILIARES
        // ============================================================

        // Clic en la fila -> Mostrar descripción abajo
        private void PropGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PropGrid.SelectedItem is PropertyItem item)
            {
                DescTitle.Text = item.Name;
                DescText.Text = string.IsNullOrEmpty(item.Description)
                    ? "Sin descripción disponible."
                    : item.Description;
            }
            else
            {
                DescTitle.Text = "";
                DescText.Text = "";
            }
        }

        // Botón Cerrar (X)
        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        // Color Picker (Popup)
        private void OnColorPickedFromPopup(string hexColor)
        {
            if (PropGrid.SelectedItem is PropertyItem item)
            {
                item.Value = hexColor;
                ApplyChange(item);
            }
        }

        // File/Font Picker (Dialogs)
        private void OnDialogButtonClick(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is PropertyItem item)
            {
                if (item.Type == PropertyType.File) HandleFilePicker(item);
                else if (item.Type == PropertyType.Font) HandleFontPicker(item);
            }
        }

        private void HandleFilePicker(PropertyItem item)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes (*.bmp;*.jpg;*.png;*.ico)|*.bmp;*.jpg;*.png;*.ico|Todos (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                item.Value = dlg.FileName;
                ApplyChange(item);
            }
        }

        private void HandleFontPicker(PropertyItem item)
        {
            // Mockup simple por ahora
            MessageBox.Show("Selector de fuente simulado.\nSe aplicará 'Courier New, 12pt'.");
            item.Value = "Courier New; 12pt";
            ApplyChange(item);
        }
    }
}