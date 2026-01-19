using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VB6VisualMockupDesigner.Helpers;

namespace VB6VisualMockupDesigner.Views
{
    public partial class SettingsView : UserControl
    {
        private bool _isLoaded = false;
        private bool _isUpdatingUi = false; // Bloqueo para evitar bucles al escribir
        private TextBox _activeColorInput;  // Recuerda qué caja de texto estamos editando con el popup

        public SettingsView()
        {
            InitializeComponent();

            // 1. Cargar estado inicial
            LoadCurrentThemeState();
            _isLoaded = true;

            // 2. Suscribirse al evento del ColorPicker
            if (MyColorPicker != null)
            {
                MyColorPicker.ColorSelected += HandleColorPicked;
            }
        }

        private void LoadCurrentThemeState()
        {
            string currentThemeTag = ThemeManager.CurrentTheme.ToString();

            // Seleccionar el ítem correcto en el ComboBox
            foreach (ComboBoxItem item in CboThemes.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == currentThemeTag)
                {
                    item.IsSelected = true;
                    break;
                }
            }

            // Llenar los inputs de color y mostrar/ocultar panel
            FillCustomColorInputs();
            UpdatePanelVisibility();
        }

        private void FillCustomColorInputs()
        {
            _isUpdatingUi = true;

            // --- GRUPO 1: ENTORNO ---
            TxtColorAppBg.Text = Properties.Settings.Default.Cust_AppBg ?? "#004080";
            TxtColorPanelBg.Text = Properties.Settings.Default.Cust_PanelBg ?? "#D4D0C8";
            TxtColorBorder.Text = Properties.Settings.Default.Cust_Border ?? "#808080";

            // --- GRUPO 2: CONTENIDO ---
            TxtColorText.Text = Properties.Settings.Default.Cust_Text ?? "#000000";
            TxtColorSecText.Text = Properties.Settings.Default.Cust_SecText ?? "#606060";
            TxtColorAccent.Text = Properties.Settings.Default.Cust_Accent ?? "#000080";

            // --- GRUPO 3: CANVAS ---
            TxtColorGrid.Text = Properties.Settings.Default.Cust_Grid ?? "#808080";
            TxtColorSelection.Text = Properties.Settings.Default.Cust_Selection ?? "#40000080";

            _isUpdatingUi = false;
        }

        private void UpdatePanelVisibility()
        {
            // Solo mostramos el panel si el tema es "Personalized"
            if (CboThemes.SelectedItem is ComboBoxItem item && item.Tag.ToString() == "Personalized")
            {
                PnlCustomization.Visibility = Visibility.Visible;
            }
            else
            {
                PnlCustomization.Visibility = Visibility.Collapsed;
            }
        }

        private void CboThemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;

            if (CboThemes.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                string themeTag = selectedItem.Tag.ToString();
                ThemeManager.ThemeType type = ThemeManager.ThemeType.Dark;

                switch (themeTag)
                {
                    case "Dark": type = ThemeManager.ThemeType.Dark; break;
                    case "Light": type = ThemeManager.ThemeType.Light; break;
                    case "Personalized": type = ThemeManager.ThemeType.Personalized; break;
                    case "HighContrast": type = ThemeManager.ThemeType.HighContrast; break;
                    case "SoftRose": type = ThemeManager.ThemeType.SoftRose; break;
                    case "CyberYellow": type = ThemeManager.ThemeType.CyberYellow; break;
                    case "Blueprint": type = ThemeManager.ThemeType.Blueprint; break;
                }

                // Aplicar tema visualmente
                ThemeManager.ApplyTheme(type);

                // Guardar preferencia
                Properties.Settings.Default.AppTheme = themeTag;
                Properties.Settings.Default.Save();

                // Actualizar UI
                UpdatePanelVisibility();
            }
        }

        // ==========================================================
        // LÓGICA DE EDICIÓN EN TIEMPO REAL (Live Preview)
        // ==========================================================
        private void TxtColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded || _isUpdatingUi) return;

            var textBox = sender as TextBox;
            if (textBox == null || textBox.Tag == null) return;

            string resourceKey = textBox.Tag.ToString();
            string hexValue = textBox.Text;

            // Validación formato Hex (#RGB, #ARGB, #RRGGBB, #AARRGGBB)
            if (hexValue.StartsWith("#") && (hexValue.Length == 4 || hexValue.Length == 5 || hexValue.Length == 7 || hexValue.Length == 9))
            {
                // 1. ACTUALIZACIÓN VISUAL (LIVE PREVIEW)
                if (ThemeManager.CurrentTheme == ThemeManager.ThemeType.Personalized)
                {
                    // A. Actualizamos el color principal que estás editando
                    ThemeManager.UpdateSingleColor(resourceKey, hexValue);

                    // B. --- CORRECCIÓN CLAVE: ACTUALIZACIÓN EN CASCADA ---
                    // Si cambias un color "Padre", actualizamos manualmente a sus "Hijos"
                    // para que la interfaz no quede dispareja.

                    if (resourceKey == "PanelBackground")
                    {
                        // El Sidebar y los Headers usan el mismo color que los paneles
                        ThemeManager.UpdateSingleColor("SideBarBackground", hexValue);
                        ThemeManager.UpdateSingleColor("HeaderBackground", hexValue);
                    }
                    else if (resourceKey == "AccentColor")
                    {
                        // La marca y el estado "Presionado" siguen al color de Acento
                        ThemeManager.UpdateSingleColor("BrandColor", hexValue);
                        ThemeManager.UpdateSingleColor("PressedBrush", hexValue);

                        // Opcional: Si quieres que la selección del árbol también cambie al momento
                        // ThemeManager.UpdateSingleColor("TreeSelectedBrush", hexValue); // (O una variante con transparencia)
                    }
                    else if (resourceKey == "PrimaryText")
                    {
                        // Los iconos suelen ser del mismo color que el texto
                        ThemeManager.UpdateSingleColor("IconColor", hexValue);
                        ThemeManager.UpdateSingleColor("TreeArrowColor", hexValue);
                    }
                }

                // 2. GUARDADO EN SETTINGS (Persistencia)
                if (textBox == TxtColorAppBg) Properties.Settings.Default.Cust_AppBg = hexValue;
                else if (textBox == TxtColorPanelBg) Properties.Settings.Default.Cust_PanelBg = hexValue;
                else if (textBox == TxtColorBorder) Properties.Settings.Default.Cust_Border = hexValue;

                else if (textBox == TxtColorText) Properties.Settings.Default.Cust_Text = hexValue;
                else if (textBox == TxtColorSecText) Properties.Settings.Default.Cust_SecText = hexValue;
                else if (textBox == TxtColorAccent) Properties.Settings.Default.Cust_Accent = hexValue;

                else if (textBox == TxtColorGrid) Properties.Settings.Default.Cust_Grid = hexValue;
                else if (textBox == TxtColorSelection) Properties.Settings.Default.Cust_Selection = hexValue;

                Properties.Settings.Default.Save();
            }
        }

        // ==========================================================
        // LÓGICA DEL COLOR PICKER (POPUP)
        // ==========================================================

        // 1. Abrir Popup al hacer clic en el cuadrito de color
        private void OnColorSwatchClicked(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            // Obtenemos la referencia al TextBox asociado (guardada en el Tag del XAML)
            _activeColorInput = border.Tag as TextBox;

            if (_activeColorInput != null)
            {
                ColorPopup.IsOpen = true; // Mostrar el popup flotante
            }
        }

        // 2. Recibir el color seleccionado desde tu control ColorPicker
        private void HandleColorPicked(string hexColor)
        {
            if (_activeColorInput != null)
            {
                // Escribir el valor en el TextBox.
                // Esto disparará automáticamente TxtColor_TextChanged, 
                // lo que actualizará el tema y guardará la configuración.
                _activeColorInput.Text = hexColor;

                // Cerrar el popup
                ColorPopup.IsOpen = false;
            }
        }
    }
}