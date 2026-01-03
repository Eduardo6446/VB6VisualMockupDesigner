using System;
using System.Collections.Generic;
using System.Globalization;
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

        public PropertiesPanel()
        {
            InitializeComponent();
        }

        public void InspectObject(FrameworkElement control)
        {
            // Si es el mismo control, no recargamos todo para no cortar el flujo,
            // a menos que sea una actualización forzada (control == null)
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

            string typeName = control.Tag as string ?? control.GetType().Name;
            string name = string.IsNullOrEmpty(control.Name) ? typeName : control.Name;
            ObjectSelector.Text = $"{name} ({typeName})";

            LoadProperties();
            _isUpdating = false;
        }

        private void LoadProperties()
        {
            if (_currentControl == null) return;

            var props = new List<PropertyItem>();

            // Usamos GetSafeValue para evitar NaNs al cargar
            props.Add(new PropertyItem { Name = "Left", Value = GetSafeValue(Canvas.GetLeft(_currentControl)) });
            props.Add(new PropertyItem { Name = "Top", Value = GetSafeValue(Canvas.GetTop(_currentControl)) });
            props.Add(new PropertyItem { Name = "Width", Value = GetSafeValue(_currentControl.Width, _currentControl.ActualWidth) });
            props.Add(new PropertyItem { Name = "Height", Value = GetSafeValue(_currentControl.Height, _currentControl.ActualHeight) });

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

            // Detectar qué se editó
            if (e.EditingElement is TextBox tb && e.Row.Item is PropertyItem item)
            {
                string newValue = tb.Text;
                string propName = item.Name;

                // Usamos Dispatcher para salir del ciclo de bloqueo del DataGrid
                // Priority.Input suele ser más seguro que Render para operaciones de datos
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    bool success = ApplyPropertyChange(propName, newValue);
                    if (success)
                    {
                        PropertyChanged?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        // Si falló (valor inválido), recargamos para revertir el valor visual en el grid
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
                // Avisamos antes de tocar nada (para Undo)
                PropertyChanging?.Invoke(this, EventArgs.Empty);

                double valNum = ParseDouble(value);

                switch (propName)
                {
                    case "Left":
                        if (!double.IsNaN(valNum)) Canvas.SetLeft(_currentControl, valNum);
                        break;
                    case "Top":
                        if (!double.IsNaN(valNum)) Canvas.SetTop(_currentControl, valNum);
                        break;

                    case "Width":
                        // PROTECCIÓN: No permitir ancho menor a 10
                        if (!double.IsNaN(valNum))
                            _currentControl.Width = Math.Max(10, valNum);
                        break;

                    case "Height":
                        // PROTECCIÓN: No permitir alto menor a 10
                        if (!double.IsNaN(valNum))
                            _currentControl.Height = Math.Max(10, valNum);
                        break;

                    case "Caption":
                        if (_currentControl is ContentControl cc) cc.Content = value;
                        if (_currentControl is TextBlock lbl) lbl.Text = value;
                        if (_currentControl is GroupBox gb) gb.Header = value;
                        break;

                    case "Text":
                        if (_currentControl is TextBox txt) txt.Text = value;
                        break;

                    default:
                        return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private double ParseDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return double.NaN;

            // Intentamos parsear punto, luego coma, luego sistema actual
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out double result2))
                return result2;

            return double.NaN; // Retornamos NaN si falla, para no poner 0
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