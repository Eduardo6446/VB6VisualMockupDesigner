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
using System.Windows.Shapes;

namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Interaction logic for StartScreen.xaml
    /// </summary>
    public partial class StartScreen : Window
    {
        public StartScreen()
        {
            InitializeComponent();
            LoadRecentMockups();
        }

        // Clase simple para poblar la UI de ejemplo
        public class RecentFile
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string Date { get; set; }
        }

        private void LoadRecentMockups()
        {
            // AQUÍ cargarías desde un JSON o Settings en el futuro
            var dummyData = new List<RecentFile>
            {
                new RecentFile { Name = "frmLogin.frm", Path = "C:\\Sistemas\\VentasLegacy", Date = "Hoy, 10:30 AM" },
                new RecentFile { Name = "frmFacturacion.frm", Path = "C:\\Sistemas\\VentasLegacy", Date = "Ayer, 4:15 PM" },
                new RecentFile { Name = "frmClientes_Old.frm", Path = "D:\\Backups\\2005", Date = "23/12/2025" }
            };

            //RecentProjectsList.ItemsSource = dummyData;
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            // Abrir el editor vacío
            MainWindow editor = new MainWindow();
            editor.Show();
            this.Close();
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para abrir OpenFileDialog y pasarle el archivo al MainWindow
            // Por ahora solo abrimos el editor
            MainWindow editor = new MainWindow();
            editor.Show();
            this.Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // Permitir arrastrar la ventana (ya que quitamos el borde nativo)
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
