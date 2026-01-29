using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Helpers;

namespace VB6VisualMockupDesigner.Views
{
    public partial class StringListEditorWindow : Window
    {
        public string ResultText { get; private set; }

        public StringListEditorWindow(string currentContent)
        {
            InitializeComponent();

            // Asignar texto y colocar cursor al final
            TxtContent.Text = currentContent;
            TxtContent.CaretIndex = TxtContent.Text.Length;

            // Evento para actualizar contador
            TxtContent.TextChanged += (s, e) => UpdateLineCount();

            // Foco inicial
            Loaded += (s, e) => {
                TxtContent.Focus();
                UpdateLineCount();
            };
            RefreshUserPresetsMenu();
        }

        private void UpdateLineCount()
        {
            int lines = TxtContent.LineCount;
            // A veces LineCount reporta -1 si no ha renderizado
            if (lines < 0) lines = 0;
            // Ajuste visual para texto vacío
            if (string.IsNullOrEmpty(TxtContent.Text)) lines = 0;

            if (LblLineCount != null)
                LblLineCount.Text = $"{lines} elementos";
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ResultText = TxtContent.Text;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void BtnInsertMenu_Click(object sender, RoutedEventArgs e)
        {
            // Abrir el menú contextual asociado al botón
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void AppendText(string text)
        {
            if (!string.IsNullOrEmpty(TxtContent.Text) && !TxtContent.Text.EndsWith("\n"))
            {
                TxtContent.AppendText(Environment.NewLine);
            }
            TxtContent.AppendText(text);
            TxtContent.Focus();
            TxtContent.CaretIndex = TxtContent.Text.Length;
            UpdateLineCount(); // Forzar actualización del contador
        }

        private void InsertDays_Click(object sender, RoutedEventArgs e)
        {
            string days = "Lunes\r\nMartes\r\nMiércoles\r\nJueves\r\nViernes\r\nSábado\r\nDomingo";
            AppendText(days);
        }

        private void InsertMonths_Click(object sender, RoutedEventArgs e)
        {
            string months = "Enero\r\nFebrero\r\nMarzo\r\nAbril\r\nMayo\r\nJunio\r\nJulio\r\nAgosto\r\nSeptiembre\r\nOctubre\r\nNoviembre\r\nDiciembre";
            AppendText(months);
        }

        private void InsertNumbers10_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            for (int i = 1; i <= 10; i++) sb.AppendLine(i.ToString());
            AppendText(sb.ToString().TrimEnd());
        }

        private void InsertNumbers09_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            for (int i = 0; i <= 9; i++) sb.AppendLine(i.ToString());
            AppendText(sb.ToString().TrimEnd());
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            TxtContent.Clear();
            UpdateLineCount();
            TxtContent.Focus();
        }

        private void InsertAlphabet_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            // Generar caracteres ASCII de A a Z
            for (char c = 'A'; c <= 'Z'; c++)
            {
                sb.AppendLine(c.ToString());
            }
            AppendText(sb.ToString().TrimEnd());
        }

        // OPCIÓN 2: Carga Dinámica desde TXT
        private void LoadFromFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Importar lista de texto",
                Filter = "Archivos de Texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                CheckFileExists = true
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    // Leer el archivo con la codificación por defecto (UTF-8 suele funcionar bien)
                    string fileContent = System.IO.File.ReadAllText(dlg.FileName);

                    // Opcional: Limpiar retornos de carro extraños si vienen de sistemas viejos
                    fileContent = fileContent.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);

                    AppendText(fileContent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo cargar el archivo:\n{ex.Message}", "Error de Importación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            string content = TxtContent.Text.Trim();
            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("No hay texto para guardar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Usamos tu diálogo existente
            var dialog = new SimpleInputDialog("Nombre del nuevo Preset:", "Mi Lista");
            dialog.Owner = this; // Centrar sobre esta ventana

            if (dialog.ShowDialog() == true)
            {
                string name = dialog.Answer;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    try
                    {
                        PresetManager.SavePreset(name, content);
                        RefreshUserPresetsMenu(); // Recargar menú
                        MessageBox.Show($"Preset '{name}' guardado correctamente.", "Éxito");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al guardar: {ex.Message}", "Error");
                    }
                }
            }
        }

        private void RefreshUserPresetsMenu()
        {
            MnuUserPresets.Items.Clear();

            var presets = PresetManager.LoadPresets();

            if (presets.Count == 0)
            {
                MnuUserPresets.Items.Add(new MenuItem { Header = "(Vacío)", IsEnabled = false });
            }
            else
            {
                foreach (var p in presets)
                {
                    var item = new MenuItem { Header = p.Name, Tag = p.Content };
                    item.Click += UserPresetItem_Click;

                    // Opcional: Agregar botón de borrar con clic derecho o submenú
                    // Por ahora lo mantenemos simple: Clic izquierdo inserta.

                    MnuUserPresets.Items.Add(item);
                }
            }
        }

        private void UserPresetItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Tag is string content)
            {
                AppendText(content);
            }
        }
    }
}