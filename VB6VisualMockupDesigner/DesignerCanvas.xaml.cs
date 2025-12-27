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
    /// Interaction logic for DesignerCanvas.xaml
    /// </summary>
    public partial class DesignerCanvas : UserControl
    {
        public DesignerCanvas()
        {
            InitializeComponent();
        }

        // Propiedad para establecer el título del formulario simulado
        public string FormTitle
        {
            get { return FormTitleText.Text; }
            set { FormTitleText.Text = value; }
        }

        // Método para obtener el Canvas donde agregaremos controles
        public Canvas GetDesignSurface()
        {
            return DesignSurface;
        }

        // Deseleccionar al hacer clic fuera (opcional para el futuro)
        private void Grid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Aquí implementarás la lógica para quitar selección de controles
            // Keyboard.ClearFocus();
        }

        private void DesignSurface_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ControlToolboxItem"))
            {
                string controlType = e.Data.GetData("ControlToolboxItem") as string;
                Point dropPosition = e.GetPosition(DesignSurface);

                // Crear el control visual
                UIElement newControl = CreateRetroControl(controlType);

                if (newControl != null)
                {
                    // Posicionar en el Canvas
                    Canvas.SetLeft(newControl, SnapToGrid(dropPosition.X));
                    Canvas.SetTop(newControl, SnapToGrid(dropPosition.Y));

                    DesignSurface.Children.Add(newControl);
                }
            }
        }

        // AYUDA: Ajustar a la rejilla de 8px (Típico VB6)
        private double SnapToGrid(double val)
        {
            return Math.Round(val / 8.0) * 8.0;
        }

        // FÁBRICA DE CONTROLES RETRO (Simulación Visual)
        private UIElement CreateRetroControl(string type)
        {
            Control control = null;

            switch (type)
            {
                case "CommandButton":
                    control = new Button
                    {
                        Content = "Command1",
                        Width = 121,
                        Height = 33, // Tamaños default VB6
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8")),
                        BorderThickness = new Thickness(2),
                        FontFamily = new FontFamily("Microsoft Sans Serif"),
                        FontSize = 11
                    };
                    break;

                case "TextBox":
                    control = new TextBox
                    {
                        Text = "Text1",
                        Width = 121,
                        Height = 25,
                        FontFamily = new FontFamily("Microsoft Sans Serif"),
                        FontSize = 11,
                        BorderBrush = Brushes.Gray
                    };
                    break;

                case "Label":
                    control = new Label
                    {
                        Content = "Label1",
                        Width = 121,
                        Height = 25,
                        FontFamily = new FontFamily("Microsoft Sans Serif"),
                        FontSize = 11,
                        Padding = new Thickness(0) // Label VB6 no tiene mucho padding
                    };
                    break;

                case "CheckBox":
                    control = new CheckBox
                    {
                        Content = "Check1",
                        Width = 121,
                        Height = 25,
                        FontFamily = new FontFamily("Microsoft Sans Serif"),
                        FontSize = 11
                    };
                    break;

                // Agregar más casos según necesites...
                default:
                    // Placeholder para controles no implementados
                    control = new Button { Content = type, Width = 100, Height = 30, Background = Brushes.Red };
                    break;
            }

            return control;
        }
    }
}
