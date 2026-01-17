using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VB6VisualMockupDesigner.Models;
using VB6VisualMockupDesigner.Controls; // Para VB6Data

namespace VB6VisualMockupDesigner.Helpers
{
    public static class PropertyManager
    {
        // Opciones reusables para Enums
        private static List<string> BoolOptions = new List<string> { "True", "False" };
        private static List<string> AlignOptions = new List<string> { "0 - Left Justify", "1 - Right Justify", "2 - Center" };
        private static List<string> BorderOptions = new List<string> { "0 - None", "1 - Fixed Single" };
        private static List<string> AppearanceOptions = new List<string> { "0 - Flat", "1 - 3D" };

        public static List<PropertyItem> GetPropertiesFor(FrameworkElement ctrl)
        {
            var list = new List<PropertyItem>();

            // --- CATEGORÍA: MISC ---
            list.Add(new PropertyItem
            {
                Name = "(Name)",
                Value = ctrl.Name,
                Category = "Misc",
                Description = "Devuelve o establece el nombre utilizado en el código para identificar un objeto."
            });

            // --- CATEGORÍA: POSITION ---
            list.Add(new PropertyItem
            {
                Name = "Left",
                Value = (int)Canvas.GetLeft(ctrl),
                Category = "Position",
                Type = PropertyType.Number,
                Description = "Devuelve o establece la distancia entre el borde interno izquierdo de un objeto y el borde izquierdo de su contenedor."
            });

            list.Add(new PropertyItem
            {
                Name = "Top",
                Value = (int)Canvas.GetTop(ctrl),
                Category = "Position",
                Type = PropertyType.Number,
                Description = "Devuelve o establece la distancia entre el borde interno superior de un objeto y el borde superior de su contenedor."
            });

            list.Add(new PropertyItem
            {
                Name = "Width",
                Value = (int)ctrl.Width,
                Category = "Position",
                Type = PropertyType.Number,
                Description = "Devuelve o establece el ancho de un objeto."
            });

            list.Add(new PropertyItem
            {
                Name = "Height",
                Value = (int)ctrl.Height,
                Category = "Position",
                Type = PropertyType.Number,
                Description = "Devuelve o establece el alto de un objeto."
            });

            // --- CATEGORÍA: APPEARANCE ---


            if (ctrl is Image || ctrl is Window) // Window tiene Icon
            {
                // Nota: En WPF Image usa 'Source', en VB6 es 'Picture'
                string currentPath = "";
                // Aquí podrías intentar leer el path si lo guardaste en el Tag o en un Helper, 
                // porque WPF convierte la imagen a memoria y pierde la ruta original.
                // Por ahora lo dejaremos vacío o leeremos del Tag si existe.

                list.Add(new PropertyItem
                {
                    Name = "Picture",
                    Value = currentPath,
                    Category = "Appearance",
                    Type = PropertyType.File,
                    Description = "Devuelve o establece un gráfico para ser mostrado en el control."
                });
            }


            if (ctrl is Control c)
            {
                string fontInfo = $"{c.FontFamily}; {c.FontSize}pt";
                if (c.FontWeight == FontWeights.Bold) fontInfo += "; Bold";

                list.Add(new PropertyItem
                {
                    Name = "Font",
                    Value = fontInfo,
                    Category = "Appearance",
                    Type = PropertyType.Font,
                    Description = "Devuelve un objeto Font."
                });

                if (c.Background is SolidColorBrush sb)
                    list.Add(new PropertyItem
                    {
                        Name = "BackColor",
                        Value = sb.Color.ToString(),
                        Category = "Appearance",
                        Type = PropertyType.Color,
                        Description = "Devuelve o establece el color de fondo usado para mostrar texto y gráficos en un objeto."
                    });

                if (c.Foreground is SolidColorBrush sf)
                    list.Add(new PropertyItem
                    {
                        Name = "ForeColor",
                        Value = sf.Color.ToString(),
                        Category = "Appearance",
                        Type = PropertyType.Color,
                        Description = "Devuelve o establece el color de primer plano usado para mostrar texto y gráficos en un objeto."
                    });
            }

            // Caption / Text
            if (ctrl is ContentControl cc)
                list.Add(new PropertyItem
                {
                    Name = "Caption",
                    Value = cc.Content,
                    Category = "Appearance",
                    Description = "Devuelve o establece el texto que se muestra en el título de un objeto o debajo del icono."
                });

            else if (ctrl is TextBox tb)
                list.Add(new PropertyItem
                {
                    Name = "Text",
                    Value = tb.Text,
                    Category = "Appearance",
                    Description = "Devuelve o establece el texto contenido en el control."
                });

            // Visible
            list.Add(new PropertyItem
            {
                Name = "Visible",
                Value = (ctrl.Visibility == Visibility.Visible).ToString(),
                Category = "Behavior",
                Type = PropertyType.Boolean,
                Options = BoolOptions,
                Description = "Devuelve o establece un valor que determina si un objeto es visible o no."
            });

            // Enabled
            list.Add(new PropertyItem
            {
                Name = "Enabled",
                Value = ctrl.IsEnabled.ToString(),
                Category = "Behavior",
                Type = PropertyType.Boolean,
                Options = BoolOptions,
                Description = "Devuelve o establece un valor que determina si un objeto puede responder a eventos generados por el usuario."
            });

            // ... Agrega descripciones al resto de propiedades que tengas ...

            return list;
        }

        public static void ApplyProperty(FrameworkElement ctrl, PropertyItem item)
        {
            string val = item.Value?.ToString();

            switch (item.Name)
            {
                // Posición
                case "Left": Canvas.SetLeft(ctrl, ParseDouble(val)); break;
                case "Top": Canvas.SetTop(ctrl, ParseDouble(val)); break;
                case "Width": ctrl.Width = Math.Max(10, ParseDouble(val)); break;
                case "Height": ctrl.Height = Math.Max(10, ParseDouble(val)); break;

                // Apariencia
                case "Caption":
                    if (ctrl is ContentControl cc) cc.Content = val;
                    if (ctrl is TextBlock txt) txt.Text = val;
                    if (ctrl is GroupBox gb) gb.Header = val;
                    break;
                case "Text":
                    if (ctrl is TextBox t) t.Text = val;
                    break;
                case "BackColor":
                    if (ctrl is Control c) c.Background = ParseColor(val);
                    if (ctrl is Border b) b.Background = ParseColor(val);
                    // Caso especial para Frame (GroupBox en WPF)
                    if (ctrl is GroupBox g) g.Background = ParseColor(val);
                    break;
                case "ForeColor":
                    if (ctrl is Control c2) c2.Foreground = ParseColor(val);
                    if (ctrl is TextBlock t2) t2.Foreground = ParseColor(val);
                    break;

                // Alignment
                case "Alignment": ApplyAlignment(ctrl, val); break;

                // Comportamiento
                case "Visible": ctrl.Visibility = (val == "True") ? Visibility.Visible : Visibility.Hidden; break;
                case "Enabled": ctrl.IsEnabled = (val == "True"); break;

                // Misc
                case "TabIndex":
                    if (ctrl is Control cTab) cTab.TabIndex = (int)ParseDouble(val);
                    break;
                case "Tag":
                    // No sobreescribir si es info de Array interno, a menos que el usuario lo haga
                    if (!string.IsNullOrEmpty(val)) ctrl.Tag = val;
                    break;
                case "Index":
                    if (string.IsNullOrWhiteSpace(val)) VB6Data.SetIndex(ctrl, null);
                    else if (int.TryParse(val, out int i)) VB6Data.SetIndex(ctrl, i);
                    break;
                case "Picture":
                    try
                    {
                        if (ctrl is Image img)
                        {
                            // Cargar imagen desde archivo
                            var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(val));
                            img.Source = bitmap;
                        }
                        // TODO: Si es un Form (Window), cambiar el Icon o Background
                    }
                    catch { /* Ignorar errores de imagen inválida */ }
                    break;

                case "Font":
                    if (ctrl is Control f)
                    {
                        // Parsear nuestro formato simple: "Familia; Tamaño; Estilo"
                        var parts = val.Split(';');
                        if (parts.Length > 0) f.FontFamily = new FontFamily(parts[0].Trim());
                        if (parts.Length > 1 && double.TryParse(parts[1].Replace("pt", ""), out double size)) f.FontSize = size;
                        if (val.Contains("Bold")) f.FontWeight = FontWeights.Bold;
                        else f.FontWeight = FontWeights.Normal;
                    }
                    break;
            }
        }

        // --- HELPERS DE CONVERSIÓN ---

        private static string GetAlignFromControl(FrameworkElement ctrl)
        {
            TextAlignment ta = TextAlignment.Left;
            if (ctrl is TextBox tb) ta = tb.TextAlignment;
            if (ctrl is TextBlock t) ta = t.TextAlignment;

            if (ta == TextAlignment.Right) return AlignOptions[1];
            if (ta == TextAlignment.Center) return AlignOptions[2];
            return AlignOptions[0];
        }

        private static void ApplyAlignment(FrameworkElement ctrl, string val)
        {
            TextAlignment align = TextAlignment.Left;
            if (val.Contains("1")) align = TextAlignment.Right;
            if (val.Contains("2")) align = TextAlignment.Center;

            if (ctrl is TextBox tb) tb.TextAlignment = align;
            if (ctrl is TextBlock t) t.TextAlignment = align;
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
            catch { return Brushes.White; }
        }


        
    }
}