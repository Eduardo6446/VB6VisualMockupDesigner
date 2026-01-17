using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Text.RegularExpressions;
using VB6VisualMockupDesigner.Models;
using VB6VisualMockupDesigner.Helpers; // Para VB6Data

namespace VB6VisualMockupDesigner.Views
{
    public partial class PropertiesPanel : UserControl
    {
        private FrameworkElement _currentControl;
        private bool _isUpdating = false;
        private System.ComponentModel.ICollectionView _view; // <--- NUEVO

        public event EventHandler PropertyChanging;
        public event EventHandler PropertyChanged;
        public event EventHandler CloseRequested;
        public Predicate<string> CheckNameAvailability;

        public PropertiesPanel()
        {
            InitializeComponent();
        }

        // ==========================================
        // CARGA DE PROPIEDADES
        // ==========================================
        public void InspectObject(FrameworkElement control)
        {
            // Evitar recargas innecesarias
            if (_currentControl == control && control != null && !_isUpdating) return;

            _currentControl = control;
            _isUpdating = true;

            if (control == null)
            {
                ObjectSelector.Text = "";
                PropGrid.ItemsSource = null;
                _isUpdating = false;
                return;
            }

            // Título del Combo
            string typeName = control.Tag as string ?? control.GetType().Name;
            typeName = typeName.Replace("Box", "").Replace("Button", "Btn"); // Alias cortos tipo VB6
            string name = string.IsNullOrEmpty(control.Name) ? "" : control.Name;
            ObjectSelector.Text = $"{name} ({typeName})";

            // GENERAR LA LISTA DE PROPIEDADES
            var props = PropertyManager.GetPropertiesFor(control);

            // Asignar al Grid
            ListCollectionView view = new ListCollectionView(props);

            _view = new ListCollectionView(props);

            _view.Filter = FilterProperties;

            // Agrupar por Categoría si el botón está activado
            if (BtnCategorized.IsChecked == true)
            {
                view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Category", System.ComponentModel.ListSortDirection.Ascending));
            }
            view.SortDescriptions.Add(new System.ComponentModel.SortDescription("Name", System.ComponentModel.ListSortDirection.Ascending));

            PropGrid.ItemsSource = view;
            _isUpdating = false;
        }

        // ==========================================
        // GUARDADO DE CAMBIOS
        // ==========================================
        private void PropGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Nota: Este evento dispara antes de que el binding actualice el source en algunos casos.
            // Usamos un pequeño delay para leer el valor actualizado del PropertyItem

            if (_isUpdating || _currentControl == null) return;

            if (e.Row.Item is PropertyItem item)
            {
                // Esperamos a que el binding termine
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ApplyChange(item);
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }

        private void ApplyChange(PropertyItem item)
        {
            try
            {
                // 1. Notificar inicio de cambio (Undo/Redo)
                PropertyChanging?.Invoke(this, EventArgs.Empty);

                // 2. Validar Nombre Especialmente
                if (item.Name == "(Name)")
                {
                    string newName = item.Value.ToString();
                    if (!IsValidVb6Name(newName))
                    {
                        MessageBox.Show("Nombre inválido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        InspectObject(_currentControl); // Revertir
                        return;
                    }
                    if (CheckNameAvailability != null && !CheckNameAvailability(newName))
                    {
                        InspectObject(_currentControl); // Revertir
                        return;
                    }
                    _currentControl.Name = newName;
                    InspectObject(_currentControl); // Actualizar UI
                }
                else
                {
                    // 3. Aplicar Propiedad Genérica
                    PropertyManager.ApplyProperty(_currentControl, item);
                }

                // 4. Notificar fin (Refrescar adornos visuales)
                PropertyChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // Si falla, revertimos la UI
                // MessageBox.Show($"Error aplicando propiedad: {ex.Message}");
                _isUpdating = true;
                InspectObject(_currentControl);
            }
        }

        // ==========================================
        // UI HELPERS
        // ==========================================
        private void CloseBtn_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle logic for sorting buttons
            if (sender == BtnCategorized) BtnAlphabetical.IsChecked = false;
            else BtnCategorized.IsChecked = false;

            // Recargar para aplicar orden
            InspectObject(_currentControl);
        }

        private bool IsValidVb6Name(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9_]*$");
        }

        // Evento cuando seleccionas una fila en la grilla
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


        // Método que recibe el color desde el UserControl ColorPicker
        private void OnColorPickedFromPopup(string hexColor)
        {
            // 1. Buscamos qué fila originó esto.
            // Como el evento viene de un Popup, es un poco truculento encontrar el DataContext original.
            // Pero WPF es inteligente: el 'sender' será el ColorPicker.
            // Y su DataContext heredado debería ser el 'PropertyItem' de la fila.

            if (PropGrid.SelectedItem is PropertyItem item)
            {
                // Actualizamos el valor del item (esto actualiza el TextBox visualmente)
                item.Value = hexColor;

                // Forzamos la aplicación del cambio al control real
                ApplyChange(item);

                // Cerramos el popup (El popup se cierra solo al hacer click fuera, 
                // pero visualmente ya seleccionamos)
                // Nota: Para cerrar el Popup programáticamente necesitaríamos referencia al ToggleButton,
                // pero al cambiar el foco suele cerrarse solo por StaysOpen="False".
            }
        }

        private void OnDialogButtonClick(object sender, RoutedEventArgs e)
        {
            // Recuperar el ítem de la fila donde se hizo clic
            if ((sender as FrameworkElement)?.DataContext is PropertyItem item)
            {
                if (item.Type == PropertyType.File)
                {
                    HandleFilePicker(item);
                }
                else if (item.Type == PropertyType.Font)
                {
                    HandleFontPicker(item);
                }
            }
        }

        private void HandleFilePicker(PropertyItem item)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar imagen",
                Filter = "Imágenes (*.bmp;*.jpg;*.png;*.ico)|*.bmp;*.jpg;*.png;*.ico|Todos los archivos (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Guardamos la ruta completa (o relativa si prefieres)
                item.Value = openFileDialog.FileName;
                ApplyChange(item);
            }
        }

        private void HandleFontPicker(PropertyItem item)
        {
            // TODO: Lo ideal aquí es usar System.Windows.Forms.FontDialog
            // Como estamos en WPF puro por ahora, haremos un mock simple.

            // Ejemplo de cambio rápido para probar:
            MessageBox.Show("Aquí se abriría el selector de fuentes.\nPor ahora, cambiaremos a 'Courier New, 12pt' como prueba.", "Font Picker Mockup");

            item.Value = "Courier New; 12pt; Bold"; // Formato simulado
            ApplyChange(item);
        }

        // Evento cuando escribes en la caja de texto
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Pedimos a la vista que se refresque (ejecute el filtro de nuevo)
            _view?.Refresh();
        }

        // Predicado de Filtrado (Devuelve true si se debe mostrar)
        private bool FilterProperties(object obj)
        {
            // Si no hay texto, mostrar todo
            if (string.IsNullOrWhiteSpace(SearchBox.Text)) return true;

            var prop = obj as PropertyItem;
            if (prop == null) return false;

            // Buscar si el nombre contiene el texto (Ignorando mayúsculas/minúsculas)
            return prop.Name.IndexOf(SearchBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }


        // 1. Agregar este evento para avisar al mundo exterior
        public event Action<FrameworkElement> ObjectSelectedFromList;

        private bool _ignoreComboEvents = false; // Evita bucles infinitos

        // 2. Modificar InspectObject para recibir la lista de controles (Opcional)
        // Pero mejor creamos un método dedicado para actualizar la lista:

        public void UpdateObjectList(IEnumerable<FrameworkElement> controls, FrameworkElement selected)
        {
            _ignoreComboEvents = true;

            ObjectSelector.ItemsSource = null;

            // Usamos la clase ControlItem en lugar de anónimos
            var comboItems = controls.Select(c => new ControlItem
            {
                Name = string.IsNullOrEmpty(c.Name) ? "[Sin Nombre]" : c.Name,
                Type = c.GetType().Name.Replace("Box", "").Replace("Button", "Btn"),
                Control = c
            }).OrderBy(x => x.Name).ToList();

            ObjectSelector.ItemsSource = comboItems;

            // Decirle al combo qué propiedad mostrar
            ObjectSelector.DisplayMemberPath = "Name";
            // Opcional: Si borras esta línea, usará el ToString() que definimos arriba: "Command1 (CommandBtn)"

            // Seleccionar el actual
            if (selected != null)
            {
                var itemToSelect = comboItems.FirstOrDefault(x => x.Control == selected);
                ObjectSelector.SelectedItem = itemToSelect;
            }

            _ignoreComboEvents = false;
        }

        // 3. Evento cuando el usuario cambia el combo manualmente
        private void ObjectSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignoreComboEvents) return;

            // AHORA SÍ: Cast seguro a nuestra clase
            if (ObjectSelector.SelectedItem is ControlItem item)
            {
                // Avisar al MainWindow
                ObjectSelectedFromList?.Invoke(item.Control);
            }
        }

    }
}