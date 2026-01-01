using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace VB6VisualMockupDesigner
{
    public partial class PropertiesPanel : UserControl
    {
        private FrameworkElement _currentControl;
        private bool _isUpdating = false;

        // Evento para notificar que algo cambió (útil para actualizar los puntos de selección en el Canvas)
        public event EventHandler PropertyChanged;

        public event EventHandler CloseRequested;

        public PropertiesPanel()
        {
            InitializeComponent();
        }

        public void InspectObject(FrameworkElement control)
        {
            _currentControl = control;
            _isUpdating = true; // Pausar eventos

            if (control == null)
            {
                ObjectSelector.Text = "";
                PropGrid.ItemsSource = null;
                _isUpdating = false;
                return;
            }

            // Nombre y Tipo (Usamos el Tag o el Tipo de clase)
            string typeName = control.Tag as string ?? control.GetType().Name;
            // Si el control tiene nombre en el XAML (x:Name), úsalo, sino usa el Tipo
            string name = string.IsNullOrEmpty(control.Name) ? typeName : control.Name;

            ObjectSelector.Text = $"{name} ({typeName})";

            var props = new List<PropertyItem>();

            // 1. Propiedades de Diseño (Redondeamos a 0 decimales para limpieza)
            props.Add(new PropertyItem { Name = "Left", Value = Math.Round(Canvas.GetLeft(control)) });
            props.Add(new PropertyItem { Name = "Top", Value = Math.Round(Canvas.GetTop(control)) });
            props.Add(new PropertyItem { Name = "Width", Value = Math.Round(control.Width) });
            props.Add(new PropertyItem { Name = "Height", Value = Math.Round(control.Height) });

            // 2. Propiedades Específicas
            if (control is ContentControl cc) // Button, Label, Frame
            {
                props.Add(new PropertyItem { Name = "Caption", Value = cc.Content });
            }
            else if (control is TextBox tb) // TextBox
            {
                props.Add(new PropertyItem { Name = "Text", Value = tb.Text });
            }
            else if (control is TextBlock txt) // TextBlock (usado en algunos placeholders)
            {
                props.Add(new PropertyItem { Name = "Caption", Value = txt.Text });
            }

            // 3. Propiedades Visuales (Color de fondo simple)
            if (control is Control c && c.Background != null)
            {
                props.Add(new PropertyItem { Name = "BackColor", Value = c.Background.ToString() });
            }

            PropGrid.ItemsSource = props;
            _isUpdating = false;
        }

        // Se dispara al terminar de editar una celda
        private void PropGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (_isUpdating || _currentControl == null) return;

            // Obtenemos la propiedad y el control de edición (TextBox)
            if (e.Row.Item is PropertyItem item && e.EditingElement is TextBox tb)
            {
                string newValue = tb.Text;
                bool success = ApplyPropertyChange(item.Name, newValue);

                // Si el cambio fue visual (tamaño/pos), notificamos
                if (success) PropertyChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool ApplyPropertyChange(string propName, string value)
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
                        if (_currentControl is TextBlock lbl) lbl.Text = value;
                        if (_currentControl is GroupBox gb) gb.Header = value; // Para Frames
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
                // Si el usuario escribe texto en un campo numérico, ignoramos el cambio
                return false;
            }
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