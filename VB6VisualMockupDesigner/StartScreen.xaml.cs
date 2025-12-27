using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VB6VisualMockupDesigner.Models;

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
            MainWindow editor = new MainWindow();
            
            if (!string.IsNullOrEmpty(filePath))
            {
                // ASUMIENDO QUE TIENES UN MÉTODO PÚBLICO EN MainWindow PARA CARGAR
                // editor.LoadFile(filePath); 
                
                // Actualizamos la lista de recientes "just in case" para refrescar la fecha
                RecentFilesManager.AddToRecents(filePath);
            }

            editor.Show();
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
