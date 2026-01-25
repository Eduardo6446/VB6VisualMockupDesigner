using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; // Necesario para actualizar la lista al importar
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VB6VisualMockupDesigner.Views
{
    /// <summary>
    /// Lógica de interacción para FontPickerWindow.xaml
    /// </summary>
    public partial class FontPickerWindow : Window
    {
        // Propiedad que leerá el PropertiesPanel al cerrar con OK
        public string ResultString { get; private set; }

        private bool _isLoading = true;

        // Colección observable para que la UI se entere cuando agregamos una fuente importada
        private ObservableCollection<FontFamily> _fontCollection;

        public FontPickerWindow(string currentFontString)
        {
            InitializeComponent();

            // Centrar sobre la ventana principal si es posible
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                this.Owner = Application.Current.MainWindow;
            }

            LoadData();
            ParseCurrentString(currentFontString);

            _isLoading = false;
            UpdatePreview();
        }

        private void LoadData()
        {
            // 1. Cargar Fuentes del Sistema
            var systemFonts = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
            _fontCollection = new ObservableCollection<FontFamily>(systemFonts);
            LstFonts.ItemsSource = _fontCollection;

            // 2. Cargar Tamaños Estándar (Estilo Office/VB6)
            var sizes = new List<string> {
                "8", "9", "10", "11", "12", "14", "16", "18", "20", "22", "24", "26", "28", "36", "48", "72"
            };
            LstSizes.ItemsSource = sizes;
        }

        // ================================================
        // LÓGICA DE IMPORTACIÓN DE FUENTES
        // ================================================
        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos de Fuente (*.ttf;*.otf)|*.ttf;*.otf",
                Title = "Importar Fuente Personalizada"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    Uri fontUri = new Uri(dlg.FileName);

                    // Intentamos leer los metadatos de la fuente para obtener el nombre real de la familia
                    // (El nombre del archivo no siempre coincide con el nombre de la fuente)
                    GlyphTypeface glyphTypeface = new GlyphTypeface(fontUri);

                    // Buscamos el nombre en inglés (LCID 1033) o el primero disponible
                    string familyName = glyphTypeface.FamilyNames.Values.FirstOrDefault();

                    if (string.IsNullOrEmpty(familyName))
                    {
                        // Fallback: Usar nombre de archivo si no tiene metadatos legibles
                        familyName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                    }

                    // Creamos la FontFamily dinámica.
                    // Sintaxis WPF para fuentes locales: "file:///Ruta/archivo.ttf#NombreFamilia"
                    var newFont = new FontFamily(fontUri, "./#" + familyName);

                    // Insertar al inicio para que el usuario la vea de inmediato
                    _fontCollection.Insert(0, newFont);

                    // Seleccionar automáticamente
                    LstFonts.SelectedItem = newFont;
                    LstFonts.ScrollIntoView(newFont);

                    MessageBox.Show($"Fuente '{familyName}' cargada correctamente.\nAhora puede usarla en este diseño.",
                                    "Fuente Importada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar la fuente:\n{ex.Message}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ================================================
        // PARSEO Y ESTADO INICIAL
        // ================================================
        private void ParseCurrentString(string fontStr)
        {
            // Formato esperado: "Family; Sizept; Style..."
            if (string.IsNullOrEmpty(fontStr)) fontStr = "Microsoft Sans Serif; 8.25";

            var parts = fontStr.Split(';');

            // 1. Familia
            string family = parts.Length > 0 ? parts[0].Trim() : "Microsoft Sans Serif";

            // Buscamos en la colección. La comparación es flexible para soportar fuentes importadas previamente.
            var foundFont = _fontCollection.FirstOrDefault(f =>
                f.Source.Equals(family, StringComparison.InvariantCultureIgnoreCase) ||
                (f.FamilyNames.Values != null && f.FamilyNames.Values.Contains(family))
            );

            if (foundFont != null)
            {
                LstFonts.SelectedItem = foundFont;
                LstFonts.ScrollIntoView(foundFont);
            }
            else
            {
                // Si no se encuentra (ej: fuente desinstalada), seleccionar la primera o default
                if (LstFonts.Items.Count > 0) LstFonts.SelectedIndex = 0;
            }

            // 2. Tamaño
            string size = "8";
            if (parts.Length > 1) size = parts[1].ToLower().Replace("pt", "").Trim();

            // Intentar matchear con la lista predefinida
            // Parseamos a double y luego floor para quitar decimales raros (8.25 -> 8) para la selección de lista
            if (double.TryParse(size, out double sizeNum))
            {
                int sizeInt = (int)Math.Floor(sizeNum);
                string sizeStr = sizeInt.ToString();

                if (LstSizes.Items.Contains(sizeStr))
                {
                    LstSizes.SelectedItem = sizeStr;
                }
                else
                {
                    // Si es un tamaño no estándar, lo ponemos en el TextBox directamente
                    LstSizes.SelectedItem = null;
                    TxtSize.Text = size;
                }
            }

            // 3. Estilos (Negrita / Cursiva)
            bool isBold = fontStr.Contains("Bold");
            bool isItalic = fontStr.Contains("Italic");

            if (isBold && isItalic) LstStyles.SelectedIndex = 3;      // Negrita Cursiva
            else if (isBold) LstStyles.SelectedIndex = 2;             // Negrita
            else if (isItalic) LstStyles.SelectedIndex = 1;           // Cursiva
            else LstStyles.SelectedIndex = 0;                         // Normal

            // 4. Efectos
            if (fontStr.Contains("Strikeout") || fontStr.Contains("Strikethrough")) ChkStrike.IsChecked = true;
            if (fontStr.Contains("Underline")) ChkUnderline.IsChecked = true;
        }

        // ================================================
        // ACTUALIZACIÓN VISUAL (PREVIEW)
        // ================================================
        private void UpdatePreview()
        {
            if (_isLoading) return;

            // Fuente
            if (LstFonts.SelectedItem is FontFamily ff)
            {
                LblPreview.FontFamily = ff;
                // Mostramos el nombre amigable si es posible
                TxtFont.Text = ff.Source.Contains("#") ? ff.FamilyNames.Values.FirstOrDefault() : ff.Source;
            }

            // Tamaño
            if (double.TryParse(TxtSize.Text, out double s))
            {
                // En WPF FontSize es pixeles (96dpi), en VB6 es Puntos (72dpi). 
                // Multiplicador aprox 1.33. Usamos Max 5 para que no desaparezca.
                LblPreview.FontSize = Math.Max(s * 1.33, 5);
            }

            // Estilo y Peso
            if (LstStyles.SelectedItem is ListBoxItem lbi)
            {
                string tag = lbi.Tag.ToString(); // "Bold", "Italic", "Regular", "BoldItalic"
                TxtStyle.Text = lbi.Content.ToString();

                LblPreview.FontWeight = (tag.Contains("Bold")) ? FontWeights.Bold : FontWeights.Normal;
                LblPreview.FontStyle = (tag.Contains("Italic")) ? FontStyles.Italic : FontStyles.Normal;
            }

            // Decoraciones (Tachado / Subrayado)
            var collection = new TextDecorationCollection();
            if (ChkStrike.IsChecked == true) collection.Add(TextDecorations.Strikethrough);
            if (ChkUnderline.IsChecked == true) collection.Add(TextDecorations.Underline);
            LblPreview.TextDecorations = collection;
        }

        // ================================================
        // EVENTOS DE UI
        // ================================================

        private void LstFonts_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdatePreview();

        private void LstStyles_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdatePreview();

        private void LstSizes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstSizes.SelectedItem != null)
            {
                TxtSize.Text = LstSizes.SelectedItem.ToString();
            }
            UpdatePreview();
        }

        // Si el usuario escribe manualmente en la caja de tamaño, actualizamos el preview al perder foco o cambiar
        // (Opcional: podrías agregar evento TextChanged al TextBox TxtSize en el XAML)

        private void Effect_Click(object sender, RoutedEventArgs e) => UpdatePreview();

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Construir el string de resultado para PropertyManager
            var ff = LstFonts.SelectedItem as FontFamily;

            // IMPORTANTE: Si es importada, usamos el Source completo (file:///...) para que funcione en otras partes
            string family = ff != null ? ff.Source : "Microsoft Sans Serif";
            string size = TxtSize.Text;

            var styleParts = new List<string>();

            // Estilos
            if (LstStyles.SelectedItem is ListBoxItem lbi)
            {
                string tag = lbi.Tag.ToString();
                if (tag.Contains("Bold")) styleParts.Add("Bold");
                if (tag.Contains("Italic")) styleParts.Add("Italic");
            }

            // Efectos
            if (ChkUnderline.IsChecked == true) styleParts.Add("Underline");
            if (ChkStrike.IsChecked == true) styleParts.Add("Strikethrough");

            // Ensamblar: "Familia; Tamaño pt; Estilos..."
            string extras = styleParts.Count > 0 ? "; " + string.Join("; ", styleParts) : "";
            ResultString = $"{family}; {size}pt{extras}";

            DialogResult = true; // Cierra la ventana devolviendo true
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Cierra la ventana devolviendo false
        }

        private void BtnCloseWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}