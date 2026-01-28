using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VB6VisualMockupDesigner.Models;
using VB6VisualMockupDesigner.Helpers;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Services;

namespace VB6VisualMockupDesigner.Views
{
    public partial class StartScreen : Window
    {
        public StartScreen()
        {
            InitializeComponent();
            // Asegúrate de tener definida VersionInfo en tu proyecto, o usa texto estático
            // TxtVersionTitle.Text = $"Novedades en v{VersionInfo.FullVersion}"; 
            LoadRecentsToUI();
        }

        private void LoadRecentsToUI()
        {
            var recents = RecentFilesManager.LoadRecents();

            if (recents.Count == 0)
            {
                RecentProjectsList.Visibility = Visibility.Collapsed;
                EmptyRecentState.Visibility = Visibility.Visible;
            }
            else
            {
                EmptyRecentState.Visibility = Visibility.Collapsed;
                RecentProjectsList.Visibility = Visibility.Visible;
                RecentProjectsList.ItemsSource = recents;
            }
        }

        private void RecentProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecentProjectsList.SelectedItem is RecentFile selectedFile)
            {
                OpenEditor(selectedFile.FullPath);
                RecentProjectsList.SelectedItem = null;
            }
        }

        private void OpenEditor(string filePath = null)
        {
            MainWindowModern editor = new MainWindowModern();

            if (!string.IsNullOrEmpty(filePath))
            {
                // El LoadProject de MainWindowModern es inteligente:
                // Detecta si es carpeta o archivo y actúa en consecuencia.
                editor.LoadProject(filePath);
            }

            editor.Show();
            this.Close();
        }

        // --- BOTÓN 2: ABRIR ARCHIVO (.FRM / .VBP) ---
        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "VB6 Project/Form (*.vbp;*.frm)|*.vbp;*.frm|All Files (*.*)|*.*",
                Title = "Seleccionar archivo de Visual Basic 6"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                OpenEditor(openFileDialog.FileName);
            }
        }

        // --- BOTÓN 3: ABRIR CARPETA (EL TRUCO WPF) ---
        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Seleccionar carpeta actual", // Nombre ficticio para engañar al diálogo
                Title = "Selecciona la carpeta raíz del proyecto",
                Filter = "Carpetas|*.folder" // Filtro dummy
            };

            if (dialog.ShowDialog() == true)
            {
                // Obtenemos la carpeta contenedora del "archivo ficticio"
                string folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);

                if (!string.IsNullOrEmpty(folderPath) && System.IO.Directory.Exists(folderPath))
                {
                    OpenEditor(folderPath);
                }
            }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            OpenEditor(null);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}