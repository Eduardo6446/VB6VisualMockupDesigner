using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VB6VisualMockupDesigner.Models;
using VB6VisualMockupDesigner.Controls; // Necesario para VB6Data y RetroControlFactory

namespace VB6VisualMockupDesigner.Helpers
{
    public static class PropertyManager
    {
        // ==========================================
        // LISTAS DE OPCIONES (ENUMS DE VB6)
        // ==========================================
        private static List<string> BoolOptions = new List<string> { "True", "False" };
        private static List<string> AlignOptions = new List<string> { "0 - Left Justify", "1 - Right Justify", "2 - Center" };
        private static List<string> BorderStyleOptions = new List<string> { "0 - None", "1 - Fixed Single" };
        private static List<string> BackStyleOptions = new List<string> { "0 - Transparent", "1 - Opaque" };
        private static List<string> AppearanceOptions = new List<string> { "0 - Flat", "1 - 3D" };

        // ==========================================
        // OBTENER PROPIEDADES (LECTURA)
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

            // --- CATEGORÍA: APPEARANCE (Lógica Unificada) ---

            // 1. Caption / Text
            if (ctrl is ContentControl cc)
            {
                list.Add(new PropertyItem
                {
                    Name = "Caption",
                    Value = cc.Content,
                    Category = "Appearance",
                    Description = "Texto que se muestra en el control."
                });
            }
            else if (ctrl is TextBox tb)
            {
                list.Add(new PropertyItem
                {
                    Name = "Text",
                    Value = tb.Text,
                    Category = "Appearance",
                    Description = "Texto contenido en el control."
                });
            }

            // 2. Alignment (Para Labels y TextBoxes)
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

            // 3. BackStyle (Transparente vs Opaco - Clave para Labels)
            if (ctrl is Label)
            {
                var bg = (ctrl as Control).Background;
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

            // 4. BackColor
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

                // ForeColor
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

            // 5. BorderStyle (Bordes)
            if (ctrl is Control cBorder)
            {
                // Detectamos si tiene borde chequeando el grosor
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

            // 6. Appearance (Flat vs 3D)
            if (ctrl is Control cApp)
            {
                // En WPF simulamos Flat/3D con colores de borde.
                // Si el borde es Negro -> Flat (0), Si es Gris -> 3D (1).
                // Por defecto asumimos 3D.
                string currentApp = AppearanceOptions[1];

                if (cApp.BorderBrush is SolidColorBrush bBrush && bBrush.Color == Colors.Black)
                {
                    currentApp = AppearanceOptions[0];
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

            // 7. Font
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

            // 8. Picture (Imágenes)
            if (ctrl is Image img)
            {
                list.Add(new PropertyItem
                {
                    Name = "Picture",
                    Value = "", // Difícil de recuperar ruta en WPF, se deja vacío para setear uno nuevo
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

            list.Add(new PropertyItem
            {
                Name = "Enabled",
                Value = ctrl.IsEnabled.ToString(),
                Category = "Behavior",
                Type = PropertyType.Boolean,
                Options = BoolOptions
            });

            if (ctrl is Control cTab)
            {
                list.Add(new PropertyItem { Name = "TabIndex", Value = cTab.TabIndex, Category = "Behavior", Type = PropertyType.Number });
            }

            list.Add(new PropertyItem { Name = "Tag", Value = ctrl.Tag, Category = "Misc" });

            // Index (Arrays de controles)
            int? idx = VB6Data.GetIndex(ctrl);
            list.Add(new PropertyItem
            {
                Name = "Index",
                Value = idx.HasValue ? idx.ToString() : "",
                Category = "Misc",
                Description = "Índice en la matriz de controles."
            });

            return list;
        }

        // ==========================================
        // APLICAR PROPIEDADES (ESCRITURA)
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

                // --- TEXT CONTENT ---
                case "Caption":
                    if (ctrl is ContentControl cc) cc.Content = val;
                    if (ctrl is TextBlock txt) txt.Text = val;
                    if (ctrl is GroupBox gb) gb.Header = val;
                    break;
                case "Text":
                    if (ctrl is TextBox t) t.Text = val;
                    break;

                // --- ALIGNMENT ---
                case "Alignment":
                    ApplyAlignment(ctrl, val);
                    break;

                // --- BACKSTYLE (Transparente/Opaco) ---
                case "BackStyle":
                    if (ctrl is Control cStyle)
                    {
                        if (val != null && val.Contains("0")) // 0 - Transparent
                        {
                            cStyle.Background = Brushes.Transparent;
                        }
                        else // 1 - Opaque
                        {
                            // Si cambiamos a opaco, ponemos el gris clásico de VB6 por defecto
                            // a menos que el usuario cambie luego el BackColor.
                            cStyle.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D4D0C8"));
                        }
                    }
                    break;

                // --- COLORS ---
                case "BackColor":
                    if (ctrl is Control cColor)
                    {
                        // Si es transparente, cambiar el color no debería hacerlo opaco automáticamente en VB6 puro,
                        // pero en este diseñador es más amigable que sí pinte el color.
                        cColor.Background = ParseColor(val);
                    }
                    if (ctrl is Border bColor) bColor.Background = ParseColor(val);
                    break;

                case "ForeColor":
                    if (ctrl is Control cFore) cFore.Foreground = ParseColor(val);
                    if (ctrl is TextBlock tFore) tFore.Foreground = ParseColor(val);
                    break;

                // --- BORDER STYLE (None / Fixed Single) ---
                case "BorderStyle":
                    if (ctrl is Control cBorder)
                    {
                        if (val != null && val.Contains("1")) // 1 - Fixed Single
                        {
                            cBorder.BorderThickness = new Thickness(1);

                            // Por defecto usamos Gris (estilo 3D/Inset standard)
                            cBorder.BorderBrush = Brushes.Gray;

                            // Los labels con borde en VB6 tienen un pequeño padding interno
                            cBorder.Padding = new Thickness(2);
                        }
                        else // 0 - None
                        {
                            cBorder.BorderThickness = new Thickness(0);
                            cBorder.Padding = new Thickness(0); // Sin padding si no hay borde
                        }
                    }
                    break;

                // --- APPEARANCE (Flat / 3D) ---
                case "Appearance":
                    if (ctrl is Control cApp)
                    {
                        // Solo tiene sentido si hay borde
                        if (cApp.BorderThickness.Top > 0)
                        {
                            if (val != null && val.Contains("0")) // 0 - Flat
                            {
                                cApp.BorderBrush = Brushes.Black; // Borde negro sólido
                            }
                            else // 1 - 3D
                            {
                                cApp.BorderBrush = Brushes.Gray; // Simulación simple de borde hundido
                            }
                        }
                    }
                    break;

                // --- FONT ---
                case "Font":
                    if (ctrl is Control f)
                    {
                        try
                        {
                            var parts = val.Split(';');
                            if (parts.Length > 0) f.FontFamily = new FontFamily(parts[0].Trim());
                            if (parts.Length > 1)
                            {
                                string sizeStr = parts[1].ToLower().Replace("pt", "").Trim();
                                if (double.TryParse(sizeStr, out double size)) f.FontSize = size;
                            }
                            if (val.Contains("Bold")) f.FontWeight = FontWeights.Bold;
                            else f.FontWeight = FontWeights.Normal;
                        }
                        catch { /* Ignorar errores de parseo */ }
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
                            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Liberar archivo
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
                    ctrl.IsEnabled = (val == "True");
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
        // MÉTODOS AUXILIARES (HELPERS)
        // ==========================================

        private static string GetAlignFromControl(FrameworkElement ctrl)
        {
            // Estandarizar la lectura de alineación
            HorizontalAlignment ha = HorizontalAlignment.Left;

            if (ctrl is Control c) ha = c.HorizontalContentAlignment;
            if (ctrl is TextBox tb) ha = (HorizontalAlignment)tb.TextAlignment; // Conversión aproximada

            if (ha == HorizontalAlignment.Right) return AlignOptions[1];
            if (ha == HorizontalAlignment.Center) return AlignOptions[2];
            return AlignOptions[0]; // Default Left
        }

        private static void ApplyAlignment(FrameworkElement ctrl, string val)
        {
            HorizontalAlignment align = HorizontalAlignment.Left;
            if (val.Contains("1")) align = HorizontalAlignment.Right;
            if (val.Contains("2")) align = HorizontalAlignment.Center;

            if (ctrl is Control c) c.HorizontalContentAlignment = align;

            // Para TextBox, TextAlignment funciona mejor visualmente que ContentAlignment
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
                // Soporta Hex (#FFFFFF) y nombres (Red, Blue)
                return (SolidColorBrush)(new BrushConverter().ConvertFrom(s));
            }
            catch
            {
                return Brushes.White; // Fallback
            }
        }
    }
}