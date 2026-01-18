using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VB6VisualMockupDesigner.Models; // Requiere .NET Core 3.1 o superior (o NuGet en .NET Framework)
using VB6VisualMockupDesigner.Helpers;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Services;

namespace VB6VisualMockupDesigner.Views
{
    /// <summary>
    /// Interaction logic for StartScreen.xaml
    /// </summary>
    public partial class StartScreen : Window
    {
        public StartScreen()
        {
            InitializeComponent();
            TxtVersionTitle.Text = $"Novedades en v{VersionInfo.FullVersion}";
            LoadRecentsToUI();
        }

        // Clase simple para poblar la UI de ejemplo
        private void LoadRecentsToUI()
        {
            var recents = RecentFilesManager.LoadRecents();

            if (recents.Count == 0)
            {
                // CASO VACÍO:
                // Ocultamos la lista para que no ocupe clicks
                RecentProjectsList.Visibility = Visibility.Collapsed;

                // Mostramos el mensaje amigable
                EmptyRecentState.Visibility = Visibility.Visible;
            }
            else
            {
                // CASO CON DATOS:
                EmptyRecentState.Visibility = Visibility.Collapsed;
                RecentProjectsList.Visibility = Visibility.Visible;

                // Cargamos los datos
                RecentProjectsList.ItemsSource = recents;
            }
        }

        // Evento para cuando el usuario hace clic en un ítem de la lista
        private void RecentProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecentProjectsList.SelectedItem is RecentFile selectedFile)
            {
                OpenEditor(selectedFile.FullPath);
                
                // Limpiar selección por si vuelven a esta pantalla (si no la cierras)
                RecentProjectsList.SelectedItem = null; 
            }
        }

        // Método centralizado para abrir el editor
        private void OpenEditor(string filePath = null)
        {
            // 1. Instanciamos la NUEVA ventana moderna
            MainWindowModern editor = new MainWindowModern();

            // 2. Si hay un archivo, lo cargamos
            if (!string.IsNullOrEmpty(filePath))
            {
                editor.LoadProject(filePath);
            }

            // 3. Mostramos la nueva ventana
            editor.Show();
            

            // 4. Cerramos la pantalla de inicio
            this.Close();
        }

        // Botón "Abrir Archivo .frm"
        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "VB6 Forms (*.frm)|*.frm|All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                OpenEditor(openFileDialog.FileName);
            }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            OpenEditor(null); // Abre vacío
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
