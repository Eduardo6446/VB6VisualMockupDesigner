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
using VB6VisualMockupDesigner.Helpers;

namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private bool _isLoaded = false;

        public SettingsView()
        {
            InitializeComponent();

            // 1. Cargamos el estado visual SIN disparar eventos
            LoadCurrentThemeState();

            // 2. Marcamos que ya terminó la carga inicial
            _isLoaded = true;

            // 3. Suscribimos el evento AHORA (no en el XAML)
            CboThemes.SelectionChanged += CboThemes_SelectionChanged;
        }

        private void LoadCurrentThemeState()
        {
            // Convertimos el Enum actual a string (ej: "Light")
            string currentThemeTag = ThemeManager.CurrentTheme.ToString();

            foreach (ComboBoxItem item in CboThemes.Items)
            {
                // Comparamos el Tag del ítem con el tema actual
                if (item.Tag != null && item.Tag.ToString() == currentThemeTag)
                {
                    item.IsSelected = true;
                    return;
                }
            }
        }

        private void CboThemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Si la ventana se está cargando, no hacemos nada (evita resets)
            if (!_isLoaded) return;

            if (CboThemes.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                string themeTag = selectedItem.Tag.ToString();

                switch (themeTag)
                {
                    case "Dark":
                        ThemeManager.ApplyTheme(ThemeManager.ThemeType.Dark);
                        break;
                    case "Light":
                        ThemeManager.ApplyTheme(ThemeManager.ThemeType.Light);
                        break;
                    case "VB6":
                        ThemeManager.ApplyTheme(ThemeManager.ThemeType.VB6);
                        break;
                    case "Custom":
                        ThemeManager.ApplyTheme(ThemeManager.ThemeType.Custom);
                        break;
                }

                Properties.Settings.Default.AppTheme = themeTag;
                Properties.Settings.Default.Save();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }
    }
}
