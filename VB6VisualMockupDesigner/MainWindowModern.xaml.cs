using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Interaction logic for MainWindowModern.xaml
    /// </summary>
    public partial class MainWindowModern : Window
    {
        public MainWindowModern()
        {
            InitializeComponent();
        }

        // ============================================
        // LÓGICA DE LA BARRA DE TÍTULO PERSONALIZADA
        // ============================================
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                BtnMaximize.Content = "\xE922"; // Icono Maximizar
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                BtnMaximize.Content = "\xE923"; // Icono Restaurar
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }


        // ============================================
        // LÓGICA DE LA BARRA DE ACTIVIDAD (Lateral Izquierda)
        // ============================================
        private ToggleButton _activeSideBarButton;

        private void ActivityBar_Click(object sender, RoutedEventArgs e)
        {
            var clickedButton = sender as ToggleButton;
            if (clickedButton == null) return;

            string panelType = clickedButton.Tag.ToString();

            // Si el botón clickeado ya estaba activo, cerramos el panel
            if (clickedButton == _activeSideBarButton)
            {
                clickedButton.IsChecked = false;
                CloseSideBar();
                _activeSideBarButton = null;
                return;
            }

            // Si había otro botón activo, lo desactivamos visualmente
            if (_activeSideBarButton != null)
            {
                _activeSideBarButton.IsChecked = false;
            }

            // Activamos el nuevo botón y abrimos el panel correspondiente
            clickedButton.IsChecked = true;
            _activeSideBarButton = clickedButton;
            OpenSideBar(panelType);
        }

        private void OpenSideBar(string panelType)
        {
            // Establecemos un ancho fijo para el panel lateral
            SideBarContainer.Width = 250;

            switch (panelType)
            {
                case "Explorer":
                    SideBarTitle.Text = "EXPLORADOR DE PROYECTOS";
                    // Aquí cargarías tu UserControl del Explorador
                    SideBarContent.Content = new System.Windows.Controls.TextBlock
                    {
                        Text = "Vista del Árbol de Archivos...",
                        Foreground = System.Windows.Media.Brushes.White
                    };
                    break;
                case "Toolbox":
                    SideBarTitle.Text = "CAJA DE HERRAMIENTAS";
                    // Aquí cargarías tu UserControl del Toolbox existente
                    SideBarContent.Content = new System.Windows.Controls.TextBlock
                    {
                        Text = "Lista de Controles VB6...",
                        Foreground = System.Windows.Media.Brushes.White
                    };
                    break;
            }
        }

        private void CloseSideBar()
        {
            // Colapsamos el panel
            SideBarContainer.Width = 0;
            SideBarContent.Content = null;
        }
    }
}
