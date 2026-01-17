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

            // --- CATEGORÍA: MISC (El Nombre va primero) ---
            list.Add(new PropertyItem { Name = "(Name)", Value = ctrl.Name, Category = "Misc" });

            // --- CATEGORÍA: POSITION ---
            list.Add(new PropertyItem { Name = "Left", Value = (int)Canvas.GetLeft(ctrl), Category = "Position", Type = PropertyType.Number });
            list.Add(new PropertyItem { Name = "Top", Value = (int)Canvas.GetTop(ctrl), Category = "Position", Type = PropertyType.Number });
            list.Add(new PropertyItem { Name = "Width", Value = (int)ctrl.Width, Category = "Position", Type = PropertyType.Number });
            list.Add(new PropertyItem { Name = "Height", Value = (int)ctrl.Height, Category = "Position", Type = PropertyType.Number });

            // --- CATEGORÍA: APPEARANCE ---
            if (ctrl is Control c)
            {
                // BackColor
                if (c.Background is SolidColorBrush sb)
                    list.Add(new PropertyItem { Name = "BackColor", Value = sb.Color.ToString(), Category = "Appearance", Type = PropertyType.Color });

                // ForeColor
                if (c.Foreground is SolidColorBrush sf)
                    list.Add(new PropertyItem { Name = "ForeColor", Value = sf.Color.ToString(), Category = "Appearance", Type = PropertyType.Color });
            }

            // Caption / Text
            if (ctrl is ContentControl cc)
                list.Add(new PropertyItem { Name = "Caption", Value = cc.Content, Category = "Appearance" });
            else if (ctrl is TextBox tb)
                list.Add(new PropertyItem { Name = "Text", Value = tb.Text, Category = "Appearance" });
            else if (ctrl is TextBlock txt)
                list.Add(new PropertyItem { Name = "Caption", Value = txt.Text, Category = "Appearance" });

            // Alignment (Simulado)
            if (ctrl is TextBox || ctrl is TextBlock || ctrl is Label)
                list.Add(new PropertyItem { Name = "Alignment", Value = GetAlignFromControl(ctrl), Category = "Appearance", Type = PropertyType.Enum, Options = AlignOptions });

            // BorderStyle (Simulado)
            if (ctrl is Border || ctrl is TextBox || ctrl is Label)
                list.Add(new PropertyItem { Name = "BorderStyle", Value = "1 - Fixed Single", Category = "Appearance", Type = PropertyType.Enum, Options = BorderOptions }); // Valor dummy por ahora

            // Visible
            list.Add(new PropertyItem { Name = "Visible", Value = (ctrl.Visibility == Visibility.Visible).ToString(), Category = "Behavior", Type = PropertyType.Boolean, Options = BoolOptions });

            // Enabled
            list.Add(new PropertyItem { Name = "Enabled", Value = ctrl.IsEnabled.ToString(), Category = "Behavior", Type = PropertyType.Boolean, Options = BoolOptions });

            // --- CATEGORÍA: MISC ---
            if (ctrl is Control cTab)
                list.Add(new PropertyItem { Name = "TabIndex", Value = cTab.TabIndex, Category = "Misc", Type = PropertyType.Number });

            list.Add(new PropertyItem { Name = "Tag", Value = ctrl.Tag?.ToString() ?? "", Category = "Misc" });

            // Index (Array)
            int? idx = VB6Data.GetIndex(ctrl);
            list.Add(new PropertyItem { Name = "Index", Value = idx.HasValue ? idx.ToString() : "", Category = "Misc" });

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