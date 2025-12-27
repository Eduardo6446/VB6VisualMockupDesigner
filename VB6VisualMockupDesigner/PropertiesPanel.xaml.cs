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
    /// Interaction logic for PropertiesPanel.xaml
    /// </summary>
    public partial class PropertiesPanel : UserControl
    {
        private FrameworkElement _currentControl;
        private bool _isUpdating = false;

        public PropertiesPanel()
        {
            InitializeComponent();
        }

        // Método principal: Recibe el control seleccionado y extrae sus datos
        public void InspectObject(FrameworkElement control)
        {
            _currentControl = control;
            _isUpdating = true; // Evitar disparar eventos mientras cargamos

            if (control == null)
            {
                ObjectSelector.Text = "";
                PropGrid.ItemsSource = null;
                _isUpdating = false;
                return;
            }

            // Simulamos el nombre de objeto VB6 (ej: Command1)
            // En un futuro podrías guardar el nombre real en la propiedad .Name o .Tag
            string typeName = control.GetType().Name;
            ObjectSelector.Text = $"{typeName}1 {typeName}";

            var props = new List<PropertyItem>();

            // 1. Propiedades Comunes de Posición
            props.Add(new PropertyItem { Name = "Left", Value = Canvas.GetLeft(control) });
            props.Add(new PropertyItem { Name = "Top", Value = Canvas.GetTop(control) });
            props.Add(new PropertyItem { Name = "Width", Value = control.Width });
            props.Add(new PropertyItem { Name = "Height", Value = control.Height });

            // 2. Propiedades Específicas (Caption/Text)
            if (control is ContentControl cc) // Button, Label, CheckBox
            {
                props.Add(new PropertyItem { Name = "Caption", Value = cc.Content });
            }
            else if (control is TextBox tb) // TextBox
            {
                props.Add(new PropertyItem { Name = "Text", Value = tb.Text });
            }

            // 3. Apariencia
            if (control is Control c)
            {
                // Convertimos el Brush a string para mostrarlo simple
                props.Add(new PropertyItem { Name = "BackColor", Value = c.Background.ToString() });
            }

            PropGrid.ItemsSource = props;
            _isUpdating = false;
        }

        // Evento: Cuando el usuario termina de editar una celda
        private void PropGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (_isUpdating || _currentControl == null) return;

            // Obtenemos el item editado y el nuevo valor
            if (e.Row.Item is PropertyItem item && e.EditingElement is TextBox tb)
            {
                string newValue = tb.Text;
                ApplyPropertyChange(item.Name, newValue);
            }
        }

        private void ApplyPropertyChange(string propName, string value)
        {
            try
            {
                switch (propName)
                {
                    case "Left":
                        Canvas.SetLeft(_currentControl, double.Parse(value));
                        break;
                    case "Top":
                        Canvas.SetTop(_currentControl, double.Parse(value));
                        break;
                    case "Width":
                        _currentControl.Width = double.Parse(value);
                        break;
                    case "Height":
                        _currentControl.Height = double.Parse(value);
                        break;
                    case "Caption":
                        if (_currentControl is ContentControl cc) cc.Content = value;
                        break;
                    case "Text":
                        if (_currentControl is TextBox txt) txt.Text = value;
                        break;
                }
            }
            catch
            {
                // Ignorar valores inválidos (ej: texto en un campo numérico)
            }
        }
    }
}
