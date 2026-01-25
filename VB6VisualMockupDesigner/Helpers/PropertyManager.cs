using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VB6VisualMockupDesigner.Models;
using VB6VisualMockupDesigner.Controls; // Necesario para VB6Data

namespace VB6VisualMockupDesigner.Helpers
{
    public static class PropertyManager
    {
        // ==========================================
        // LISTAS DE OPCIONES (ENUMS SIMULADOS)
        // ==========================================
        private static List<string> BoolOptions = new List<string> { "True", "False" };
        private static List<string> AlignOptions = new List<string> { "0 - Left Justify", "1 - Right Justify", "2 - Center" };
        private static List<string> BorderStyleOptions = new List<string> { "0 - None", "1 - Fixed Single" };
        private static List<string> BackStyleOptions = new List<string> { "0 - Transparent", "1 - Opaque" };
        private static List<string> AppearanceOptions = new List<string> { "0 - Flat", "1 - 3D" };

        // ==========================================
        // 1. OBTENER PROPIEDADES (LECTURA)
        // ==========================================
        public static List<PropertyItem> GetPropertiesFor(FrameworkElement ctrl)
        {
            var list = new List<PropertyItem>();

            if (ctrl == null) return list;

            // --- CATEGORÍA: MISC ---
            list.Add(new PropertyItem
            {
                Name = "(Name)",
                Value = ctrl.Name,
                Category = "Misc",
                Description = "Devuelve o establece el nombre utilizado en el código para identificar un objeto."
            });

            // --- CATEGORÍA: POSITION ---
            list.Add(new PropertyItem { Name = "Left", Value = (int)Canvas.GetLeft(ctrl), Category = "Position", Type = PropertyType.Number });
            list.Add(new PropertyItem { Name = "Top", Value = (int)Canvas.GetTop(ctrl), Category = "Position", Type = PropertyType.Number });
            list.Add(new PropertyItem { Name = "Width", Value = (int)ctrl.Width, Category = "Position", Type = PropertyType.Number });
            list.Add(new PropertyItem { Name = "Height", Value = (int)ctrl.Height, Category = "Position", Type = PropertyType.Number });

            // --- CATEGORÍA: APPEARANCE ---

            // Caption / Text
            if (ctrl is ContentControl cc)
            {
                list.Add(new PropertyItem { Name = "Caption", Value = cc.Content, Category = "Appearance", Description = "Texto que se muestra en el control." });
            }
            else if (ctrl is TextBox tb)
            {
                list.Add(new PropertyItem { Name = "Text", Value = tb.Text, Category = "Appearance", Description = "Texto contenido en el control." });
            }
            // Agregamos soporte de lectura para TextBlocks (Timer, SSPanel, etc.)
            else if (ctrl is TextBlock txt)
            {
                list.Add(new PropertyItem { Name = "Caption", Value = txt.Text, Category = "Appearance", Description = "Texto que se muestra en el control." });
            }
            // Soporte para Border que contiene un TextBlock (algunos controles retro)
            else if (ctrl is Border b && b.Child is TextBlock tbInside)
            {
                list.Add(new PropertyItem { Name = "Caption", Value = tbInside.Text, Category = "Appearance", Description = "Texto que se muestra en el control." });
            }

            // Alignment (Para Labels y TextBoxes)
            if (ctrl is Label || ctrl is TextBox)
            {
                list.Add(new PropertyItem
                {
                    Name = "Alignment",
                    Value = GetAlignFromControl(ctrl),
                    Category = "Appearance",
                    Type = PropertyType.Enum,
                    Options = AlignOptions,
                    Description = "Alineación del texto."
                });
            }

            // BackStyle (Transparente vs Opaco - Clave para Labels)
            if (ctrl is Label)
            {
                var bg = (ctrl as Control).Background;
                // Si es nulo o Transparente -> 0. Si tiene color -> 1.
                string currentStyle = (bg == null || bg == Brushes.Transparent) ? BackStyleOptions[0] : BackStyleOptions[1];

                list.Add(new PropertyItem
                {
                    Name = "BackStyle",
                    Value = currentStyle,
                    Category = "Appearance",
                    Type = PropertyType.Enum,
                    Options = BackStyleOptions,
                    Description = "Indica si un control es transparente u opaco."
                });
            }

            // BackColor & ForeColor
            if (ctrl is Control c)
            {
                string colorHex = "#FFFFFF";
                if (c.Background is SolidColorBrush sb) colorHex = sb.Color.ToString();

                list.Add(new PropertyItem 
                { 
                    Name = "BackColor", 
                    Value = colorHex, 
                    Category = "Appearance", 
                    Type = PropertyType.Color,
                    Description = "Color de fondo."
                });

                string foreHex = "#000000";
                if (c.Foreground is SolidColorBrush sf) foreHex = sf.Color.ToString();

                list.Add(new PropertyItem 
                { 
                    Name = "ForeColor", 
                    Value = foreHex, 
                    Category = "Appearance", 
                    Type = PropertyType.Color,
                    Description = "Color del texto."
                });
            }
            // Soporte lectura de color para TextBlock sueltos (Timer, etc)
            else if (ctrl is TextBlock t)
            {
                string foreHex = "#000000";
                if (t.Foreground is SolidColorBrush sf) foreHex = sf.Color.ToString();
                list.Add(new PropertyItem { Name = "ForeColor", Value = foreHex, Category = "Appearance", Type = PropertyType.Color });
            }
            else if (ctrl is Border b)
            {
                string bgHex = "#D4D0C8";
                if (b.Background is SolidColorBrush sb) bgHex = sb.Color.ToString();
                list.Add(new PropertyItem { Name = "BackColor", Value = bgHex, Category = "Appearance", Type = PropertyType.Color });

                // Si el Border tiene un hijo TextBlock, leemos su ForeColor
                if (b.Child is TextBlock tbChild)
                {
                    string foreHex = "#000000";
                    if (tbChild.Foreground is SolidColorBrush sf) foreHex = sf.Color.ToString();
                    list.Add(new PropertyItem { Name = "ForeColor", Value = foreHex, Category = "Appearance", Type = PropertyType.Color });
                }
            }

            // BorderStyle
            if (ctrl is Control cBorder)
            {
                string currentBorder = (cBorder.BorderThickness.Top > 0) ? BorderStyleOptions[1] : BorderStyleOptions[0];
                list.Add(new PropertyItem
                {
                    Name = "BorderStyle",
                    Value = currentBorder,
                    Category = "Appearance",
                    Type = PropertyType.Enum,
                    Options = BorderStyleOptions,
                    Description = "Estilo del borde."
                });
            }

            // Appearance (Flat vs 3D)
            if (ctrl is Control cApp)
            {
                string currentApp = AppearanceOptions[1]; // Default 3D (Gris)
                if (cApp.BorderBrush is SolidColorBrush bBrush && bBrush.Color == Colors.Black)
                {
                    currentApp = AppearanceOptions[0]; // Flat (Negro)
                }

                list.Add(new PropertyItem
                {
                    Name = "Appearance",
                    Value = currentApp,
                    Category = "Appearance",
                    Type = PropertyType.Enum,
                    Options = AppearanceOptions,
                    Description = "Determina si se dibuja con efectos 3D."
                });
            }

            // Font
            if (ctrl is Control cFont)
            {
                string fontInfo = $"{cFont.FontFamily}; {cFont.FontSize}pt";
                if (cFont.FontWeight == FontWeights.Bold) fontInfo += "; Bold";

                list.Add(new PropertyItem
                {
                    Name = "Font",
                    Value = fontInfo,
                    Category = "Appearance",
                    Type = PropertyType.Font,
                    Description = "Fuente del texto."
                });
            }

            // Picture
            if (ctrl is Image img)
            {
                list.Add(new PropertyItem
                {
                    Name = "Picture",
                    Value = "",
                    Category = "Appearance",
                    Type = PropertyType.File,
                    Description = "Gráfico mostrado en el control."
                });
            }

            // --- CATEGORÍA: BEHAVIOR ---

            list.Add(new PropertyItem
            {
                Name = "Visible",
                Value = (ctrl.Visibility == Visibility.Visible).ToString(),
                Category = "Behavior",
                Type = PropertyType.Boolean,
                Options = BoolOptions
            });

            // LÓGICA ENABLED (Simulación visual)
            bool isLogicallyEnabled = ctrl.Opacity > 0.9;

            list.Add(new PropertyItem
            {
                Name = "Enabled",
                Value = isLogicallyEnabled.ToString(),
                Category = "Behavior",
                Type = PropertyType.Boolean,
                Options = BoolOptions,
                Description = "Habilita o deshabilita el control (Visualmente en modo diseño)."
            });

            if (ctrl is Control cTab)
            {
                list.Add(new PropertyItem { Name = "TabIndex", Value = cTab.TabIndex, Category = "Behavior", Type = PropertyType.Number });
            }

            list.Add(new PropertyItem { Name = "Tag", Value = ctrl.Tag, Category = "Misc" });

            int? idx = VB6Data.GetIndex(ctrl);
            list.Add(new PropertyItem
            {
                Name = "Index",
                Value = idx.HasValue ? idx.ToString() : "",
                Category = "Misc",
                Description = "Indica el índice en una matriz de controles."
            });

            return list;
        }

        // ==========================================
        // 2. APLICAR PROPIEDADES (ESCRITURA)
        // ==========================================
        public static void ApplyProperty(FrameworkElement ctrl, PropertyItem item)
        {
            string val = item.Value?.ToString();

            switch (item.Name)
            {
                // --- POSITION ---
                case "Left": Canvas.SetLeft(ctrl, ParseDouble(val)); break;
                case "Top": Canvas.SetTop(ctrl, ParseDouble(val)); break;
                case "Width": ctrl.Width = Math.Max(10, ParseDouble(val)); break;
                case "Height": ctrl.Height = Math.Max(10, ParseDouble(val)); break;

                // --- CONTENT ---
                case "Caption":
                    if (ctrl is ContentControl cc) cc.Content = val;
                    if (ctrl is TextBlock txt) txt.Text = val;
                    if (ctrl is GroupBox gb) gb.Header = val;
                    // Caso especial: Border que envuelve TextBlock (ej: Timer, SSPanel)
                    if (ctrl is Border b && b.Child is TextBlock tChild) tChild.Text = val;
                    break;

                case "Text":
                    if (ctrl is TextBox t) t.Text = val;
                    break;

                // --- ALIGNMENT ---
                case "Alignment":
                    ApplyAlignment(ctrl, val);
                    break;

                // --- BACKSTYLE (Label Transparent vs Opaque) ---
                case "BackStyle":
                    if (ctrl is Control cStyle)
                    {
                        if (val != null && val.Contains("0")) // Transparent
                        {
                            cStyle.Background = Brushes.Transparent;
                        }
                        else // Opaque
                        {
                            cStyle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8"));
                        }
                    }
                    break;

                // --- COLORS ---
                case "BackColor":
                    if (ctrl is Control cColor) cColor.Background = ParseColor(val);
                    if (ctrl is Border bColor) bColor.Background = ParseColor(val);
                    // TextBlock tiene Background también, por si acaso
                    if (ctrl is TextBlock tColor) tColor.Background = ParseColor(val);
                    break;

                case "ForeColor":
                    if (ctrl is Control cFore) cFore.Foreground = ParseColor(val);
                    if (ctrl is TextBlock tFore) tFore.Foreground = ParseColor(val); // <--- AQUI ESTÁ LA LÍNEA RESTAURADA
                    // Caso especial: Border que envuelve TextBlock
                    if (ctrl is Border bFore && bFore.Child is TextBlock tbFore) tbFore.Foreground = ParseColor(val);
                    break;

                // --- BORDER STYLE ---
                case "BorderStyle":
                    if (ctrl is Control cBorder)
                    {
                        if (val != null && val.Contains("1")) // Fixed Single
                        {
                            cBorder.BorderThickness = new Thickness(1);
                            cBorder.BorderBrush = Brushes.Gray;
                            cBorder.Padding = new Thickness(2);
                        }
                        else // None
                        {
                            cBorder.BorderThickness = new Thickness(0);
                            cBorder.Padding = new Thickness(0);
                        }
                    }
                    break;

                // --- APPEARANCE ---
                case "Appearance":
                    if (ctrl is Control cApp && cApp.BorderThickness.Top > 0)
                    {
                        if (val != null && val.Contains("0")) // Flat
                            cApp.BorderBrush = Brushes.Black;
                        else // 3D
                            cApp.BorderBrush = Brushes.Gray;
                    }
                    break;

                // --- FONT ---
                case "Font":
                    if (ctrl is Control f)
                    {
                        ApplyFontToControl(f, val);
                    }
                    // Soporte para TextBlock sueltos
                    else if (ctrl is TextBlock tx)
                    {
                        ApplyFontToTextBlock(tx, val);
                    }
                    break;

                // --- PICTURE ---
                case "Picture":
                    try
                    {
                        if (ctrl is Image img && !string.IsNullOrEmpty(val) && System.IO.File.Exists(val))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(val);
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            img.Source = bitmap;
                        }
                    }
                    catch { /* Ignorar imagen inválida */ }
                    break;

                // --- BEHAVIOR ---
                case "Visible":
                    ctrl.Visibility = (val == "True") ? Visibility.Visible : Visibility.Hidden;
                    break;

                case "Enabled":
                    // Simulación visual de Enabled
                    bool enableState = (val == "True");
                    if (enableState)
                    {
                        ctrl.Opacity = 1.0;
                    }
                    else
                    {
                        ctrl.Opacity = 0.5;
                    }
                    break;

                case "TabIndex":
                    if (ctrl is Control cTab) cTab.TabIndex = (int)ParseDouble(val);
                    break;

                case "Tag":
                    ctrl.Tag = val;
                    break;

                case "Index":
                    if (string.IsNullOrWhiteSpace(val)) VB6Data.SetIndex(ctrl, null);
                    else if (int.TryParse(val, out int i)) VB6Data.SetIndex(ctrl, i);
                    break;
            }
        }

        // ==========================================
        // HELPERS PRIVADOS
        // ==========================================

        private static void ApplyFontToControl(Control f, string val)
        {
            try
            {
                var parts = val.Split(';');
                if (parts.Length > 0) f.FontFamily = new FontFamily(parts[0].Trim());
                if (parts.Length > 1)
                {
                    string sizeStr = parts[1].ToLower().Replace("pt", "").Trim();
                    if (double.TryParse(sizeStr, out double size)) f.FontSize = Math.Max(1, size * 1.33); // *1.33 aprox pt a px
                }

                // Resetear estilos antes de aplicar
                f.FontWeight = FontWeights.Normal;
                f.FontStyle = FontStyles.Normal;

                // Como 'Control' no tiene TextDecorations nativo fácil (depende del contenido),
                // aplicamos solo Bold e Italic. 
                // Si el Control es un Label o TextBox, podríamos buscar dentro, pero en WPF básico
                // TextDecorations se heredan mejor en TextBlock.

                if (val.Contains("Bold")) f.FontWeight = FontWeights.Bold;
                if (val.Contains("Italic")) f.FontStyle = FontStyles.Italic;
            }
            catch { }
        }

        private static void ApplyFontToTextBlock(TextBlock f, string val)
        {
            try
            {
                var parts = val.Split(';');
                if (parts.Length > 0) f.FontFamily = new FontFamily(parts[0].Trim());
                if (parts.Length > 1)
                {
                    string sizeStr = parts[1].ToLower().Replace("pt", "").Trim();
                    if (double.TryParse(sizeStr, out double size)) f.FontSize = Math.Max(1, size * 1.33);
                }

                f.FontWeight = val.Contains("Bold") ? FontWeights.Bold : FontWeights.Normal;
                f.FontStyle = val.Contains("Italic") ? FontStyles.Italic : FontStyles.Normal;

                // TextDecorations
                var decos = new TextDecorationCollection();
                if (val.Contains("Underline")) decos.Add(TextDecorations.Underline);
                if (val.Contains("Strikethrough") || val.Contains("Strikeout")) decos.Add(TextDecorations.Strikethrough);
                f.TextDecorations = decos;
            }
            catch { }
        }

        private static string GetAlignFromControl(FrameworkElement ctrl)
        {
            HorizontalAlignment ha = HorizontalAlignment.Left;
            if (ctrl is Control c) ha = c.HorizontalContentAlignment;
            if (ctrl is TextBox tb) ha = (HorizontalAlignment)tb.TextAlignment;

            if (ha == HorizontalAlignment.Right) return AlignOptions[1];
            if (ha == HorizontalAlignment.Center) return AlignOptions[2];
            return AlignOptions[0];
        }

        private static void ApplyAlignment(FrameworkElement ctrl, string val)
        {
            HorizontalAlignment align = HorizontalAlignment.Left;
            if (val.Contains("1")) align = HorizontalAlignment.Right;
            if (val.Contains("2")) align = HorizontalAlignment.Center;

            if (ctrl is Control c) c.HorizontalContentAlignment = align;

            if (ctrl is TextBox tb)
            {
                if (align == HorizontalAlignment.Left) tb.TextAlignment = TextAlignment.Left;
                if (align == HorizontalAlignment.Right) tb.TextAlignment = TextAlignment.Right;
                if (align == HorizontalAlignment.Center) tb.TextAlignment = TextAlignment.Center;
            }
        }

        private static double ParseDouble(string s)
        {
            if (double.TryParse(s, out double d)) return d;
            return 0;
        }

        private static Brush ParseColor(string s)
        {
            try
            {
                return (SolidColorBrush)(new BrushConverter().ConvertFrom(s));
            }
            catch
            {
                return Brushes.White;
            }
        }
    }
}