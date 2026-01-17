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
    }
}