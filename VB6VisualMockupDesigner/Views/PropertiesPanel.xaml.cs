using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        // Lista para mantener referencia y desuscribir eventos
        private List<PropertyItem> _observedProperties = new List<PropertyItem>();

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
            // 1. Limpieza previa (Importante para no duplicar eventos)
            if (_observedProperties != null)
            {
                foreach (var prop in _observedProperties)
                {
                    prop.PropertyChanged -= OnPropertyItemChanged;
                }
                _observedProperties.Clear();
            }

            _currentControl = control;
            _isUpdating = true; // Bloqueamos actualizaciones visuales mientras cargamos

            if (control == null)
            {
                PropGrid.ItemsSource = null;
                ObjectSelector.SelectedItem = null;
                _isUpdating = false;
                return;
            }

            // 2. Obtener nuevas propiedades
            var props = PropertyManager.GetPropertiesFor(control);

            // 3. SUSCRIPCIÓN A CAMBIOS (La corrección clave)
            // Esto asegura que si cambias un ComboBox, se detecte inmediatamente.
            foreach (var prop in props)
            {
                prop.PropertyChanged += OnPropertyItemChanged;
                _observedProperties.Add(prop);
            }

            // 4. Crear vista filtrable
            _view = new ListCollectionView(props);
            _view.Filter = FilterProperties;

            ApplySortingAndGrouping();

            PropGrid.ItemsSource = _view;

            _isUpdating = false; // Desbloqueamos
        }

        // Este método se dispara automáticamente cuando el Binding actualiza el valor (ComboBox o TextBox)
        private void OnPropertyItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Value")
            {
                ApplyChange(sender as PropertyItem);
            }
        }

        // ============================================================
        // 2. LÓGICA DEL BUSCADOR (FILTER)
        // ============================================================
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _view?.Refresh();
        }

        private bool FilterProperties(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text)) return true;
            var prop = obj as PropertyItem;
            if (prop == null) return false;
            return prop.Name.IndexOf(SearchBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ============================================================
        // 3. LÓGICA DEL OBJECT SELECTOR
        // ============================================================
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
        // 4. ORDENACIÓN
        // ============================================================
        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Primitives.ToggleButton;
            if (btn == null) return;

            if (btn == BtnCategorized) BtnAlphabetical.IsChecked = false;
            else BtnCategorized.IsChecked = false;

            if (_view != null)
            {
                ApplySortingAndGrouping();
                _view.Refresh();
            }
        }

        private void ApplySortingAndGrouping()
        {
            if (_view == null) return;
            _view.GroupDescriptions.Clear();
            _view.SortDescriptions.Clear();

            if (BtnCategorized.IsChecked == true)
            {
                _view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                _view.SortDescriptions.Add(new SortDescription("Category", ListSortDirection.Ascending));
                _view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }
            else
            {
                _view.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            }
        }

        // ============================================================
        // 5. APLICAR CAMBIOS
        // ============================================================

        // Mantenemos este evento para TextBoxes, ya que fuerza la actualización del Binding
        // cuando pierden el foco o presionas Enter.
        private void PropGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var item = e.Row.Item as PropertyItem;
                if (item != null)
                {
                    // Forzar actualización del binding para TextBoxes
                    if (e.EditingElement is TextBox tb)
                    {
                        // Esto disparará OnPropertyItemChanged automáticamente
                        var binding = tb.GetBindingExpression(TextBox.TextProperty);
                        binding?.UpdateSource();
                    }
                    // Nota: No llamamos ApplyChange aquí directamente porque 
                    // OnPropertyItemChanged ya lo hará al actualizarse el source.
                }
            }
        }

        private void ApplyChange(PropertyItem item)
        {
            // Protección: No aplicar si estamos cargando el objeto inicialmente
            if (_currentControl == null || _isUpdating) return;

            // Validación de Nombre
            if (item.Name == "(Name)")
            {
                string newName = item.Value?.ToString();
                if (CheckNameAvailability != null && !CheckNameAvailability(newName))
                {
                    _isUpdating = true; // Evitar loop infinito al revertir
                    item.Value = _currentControl.Name;
                    _isUpdating = false;
                    return;
                }
            }

            PropertyChanging?.Invoke(this, EventArgs.Empty); // Snapshot para Undo

            // ¡AQUÍ OCURRE LA MAGIA!
            PropertyManager.ApplyProperty(_currentControl, item);

            PropertyChanged?.Invoke(this, EventArgs.Empty);  // Refrescar selección visual
        }

        // ============================================================
        // 6. UI AUXILIARES
        // ============================================================
        private void PropGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PropGrid.SelectedItem is PropertyItem item)
            {
                DescTitle.Text = item.Name;
                DescText.Text = string.IsNullOrEmpty(item.Description) ? "Sin descripción." : item.Description;
            }
            else
            {
                DescTitle.Text = ""; DescText.Text = "";
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

        private void OnColorPickedFromPopup(string hexColor)
        {
            if (PropGrid.SelectedItem is PropertyItem item)
            {
                item.Value = hexColor; // Esto dispara OnPropertyItemChanged -> ApplyChange
            }
        }

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
                item.Value = dlg.FileName; // Dispara ApplyChange
            }
        }

        private void HandleFontPicker(PropertyItem item)
        {
            MessageBox.Show("Selector de fuente simulado.\nSe aplicará 'Courier New, 12pt'.");
            item.Value = "Courier New; 12pt"; // Dispara ApplyChange
        }
    }
}