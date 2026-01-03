using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions; // Necesario para Regex
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace VB6VisualMockupDesigner
{
    public partial class PropertiesPanel : UserControl
    {
        private FrameworkElement _currentControl;
        private bool _isUpdating = false;

        public event EventHandler PropertyChanging;
        public event EventHandler PropertyChanged;
        public event EventHandler CloseRequested;

        // Delegado para preguntar afuera si el nombre está libre
        public Predicate<string> CheckNameAvailability;

        public PropertiesPanel()
        {
            InitializeComponent();
        }

        public void InspectObject(FrameworkElement control)
        {
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

            // Mostrar Info en el Combo Superior
            string typeName = control.Tag as string ?? control.GetType().Name;
            // Si el nombre está vacío (recién creado), sugerimos uno o mostramos vacío
            string name = string.IsNullOrEmpty(control.Name) ? "" : control.Name;
            ObjectSelector.Text = string.IsNullOrEmpty(name) ? $"[{typeName}]" : $"{name} ({typeName})";

            LoadProperties();
            _isUpdating = false;
        }

        private void LoadProperties()
        {
            if (_currentControl == null) return;

            var props = new List<PropertyItem>();

            // 0. PROPIEDAD ESPECIAL: (Name) - Va primero como en VB6
            // Usamos paréntesis para que salga arriba visualmente
            string currentName = string.IsNullOrEmpty(_currentControl.Name) ? "" : _currentControl.Name;
            props.Add(new PropertyItem { Name = "(Name)", Value = currentName });

            // 1. Diseño
            props.Add(new PropertyItem { Name = "Left", Value = GetSafeValue(Canvas.GetLeft(_currentControl)) });
            props.Add(new PropertyItem { Name = "Top", Value = GetSafeValue(Canvas.GetTop(_currentControl)) });
            props.Add(new PropertyItem { Name = "Width", Value = GetSafeValue(_currentControl.Width, _currentControl.ActualWidth) });
            props.Add(new PropertyItem { Name = "Height", Value = GetSafeValue(_currentControl.Height, _currentControl.ActualHeight) });

            // Usamos "tabCtrl" en lugar de "c" para evitar conflictos de nombres
            props.Add(new PropertyItem { Name = "TabIndex", Value = (_currentControl is Control tabCtrl) ? tabCtrl.TabIndex : 0 });

            // 2. Específicas
            if (_currentControl is ContentControl cc)
                props.Add(new PropertyItem { Name = "Caption", Value = cc.Content });
            else if (_currentControl is TextBox tb)
                props.Add(new PropertyItem { Name = "Text", Value = tb.Text });
            else if (_currentControl is TextBlock txt)
                props.Add(new PropertyItem { Name = "Caption", Value = txt.Text });

            if (_currentControl is Control c && c.Background != null)
                props.Add(new PropertyItem { Name = "BackColor", Value = c.Background.ToString() });

            PropGrid.ItemsSource = props;
        }

        private double GetSafeValue(double val, double fallback = 0)
        {
            return double.IsNaN(val) ? Math.Round(fallback) : Math.Round(val);
        }

        private void PropGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (_isUpdating || _currentControl == null) return;

            if (e.EditingElement is TextBox tb && e.Row.Item is PropertyItem item)
            {
                string newValue = tb.Text;
                string propName = item.Name;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    bool success = ApplyPropertyChange(propName, newValue);
                    if (success)
                    {
                        PropertyChanged?.Invoke(this, EventArgs.Empty);
                        // Si cambiamos el nombre, actualizamos el título del combo superior
                        if (propName == "(Name)") InspectObject(_currentControl);
                    }
                    else
                    {
                        // Si falló, recargamos para revertir el texto visualmente
                        _isUpdating = true;
                        LoadProperties();
                        _isUpdating = false;
                    }
                }), DispatcherPriority.Input);
            }
        }

        private bool ApplyPropertyChange(string propName, string value)
        {
            try
            {
                // Validación especial para NOMBRES
                if (propName == "(Name)")
                {
                    if (!IsValidVb6Name(value))
                    {
                        MessageBox.Show("Nombre inválido. Debe comenzar con una letra, no tener espacios y solo contener letras, números o guiones bajos.", "Error de Sintaxis", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return false;
                    }

                    // Verificar Unicidad (Si el nombre cambió)
                    if (value != _currentControl.Name)
                    {
                        // Preguntamos al padre si el nombre está libre (si el delegado existe)
                        if (CheckNameAvailability != null && !CheckNameAvailability(value))
                        {
                            MessageBox.Show("Ya existe un control con este nombre en el formulario.", "Nombre Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                    }

                    // Si pasa, asignamos (esto también actualiza el x:Name interno de WPF)
                    PropertyChanging?.Invoke(this, EventArgs.Empty);
                    _currentControl.Name = value;
                    return true;
                }

                // Resto de propiedades normales
                PropertyChanging?.Invoke(this, EventArgs.Empty);
                double valNum = ParseDouble(value);

                switch (propName)
                {
                    case "Left": if (!double.IsNaN(valNum)) Canvas.SetLeft(_currentControl, valNum); break;
                    case "Top": if (!double.IsNaN(valNum)) Canvas.SetTop(_currentControl, valNum); break;
                    case "Width": if (!double.IsNaN(valNum)) _currentControl.Width = Math.Max(10, valNum); break;
                    case "Height": if (!double.IsNaN(valNum)) _currentControl.Height = Math.Max(10, valNum); break;

                    case "Caption":
                        if (_currentControl is ContentControl cc) cc.Content = value;
                        if (_currentControl is TextBlock lbl) lbl.Text = value;
                        if (_currentControl is GroupBox gb) gb.Header = value;
                        break;
                    case "Text":
                        if (_currentControl is TextBox txt) txt.Text = value;
                        break;
                    case "TabIndex":
                        if (_currentControl is Control ctrlTab)
                        {
                            int index = (int)ParseDouble(value);
                            if (index >= 0) ctrlTab.TabIndex = index;
                        }
                        break;
                    default: return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Validador Estilo VB6
        private bool IsValidVb6Name(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name.Length > 40) return false; // Límite VB6
            // Regex: Empieza con letra, sigue con letras/números/guion bajo.
            return Regex.IsMatch(name, @"^[a-zA-Z][a-zA-Z0-9_]*$");
        }

        private double ParseDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return double.NaN;
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)) return result;
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out double result2)) return result2;
            return double.NaN;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public class PropertyItem
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }
    }
}