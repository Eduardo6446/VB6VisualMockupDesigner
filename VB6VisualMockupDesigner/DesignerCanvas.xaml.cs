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
    }
}
