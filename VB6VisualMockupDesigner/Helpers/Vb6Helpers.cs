using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using VB6VisualMockupDesigner.Models;

namespace VB6VisualMockupDesigner.Helpers
{
    /// <summary>
    /// Provides helper methods for parsing and converting VB6 forms and properties.
    /// </summary>
    public static class Vb6Helpers
    {
        private const double TwipsPerPixel = 15.0;

        /// <summary>
        /// Parses a VB6 form file content and creates a control model hierarchy.
        /// </summary>
        /// <param name="fileContent">The content of the VB6 .frm file.</param>
        /// <returns>A VbControlModel representing the form and its controls.</returns>
        public static VbControlModel ParseVb6Form(string fileContent)
        {
            using (StringReader reader = new StringReader(fileContent))
            {
                string line;
                VbControlModel root = null;
                Stack<VbControlModel> stack = new Stack<VbControlModel>();
                List<MenuModel> tempMenuList = new List<MenuModel>();

                while ((line = reader.ReadLine()) != null)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("'") || trimmedLine.StartsWith("Attribute") || trimmedLine.StartsWith("VERSION")) continue;

                    // 1. PROCESAR MENÚS
                    if (trimmedLine.StartsWith("Begin VB.Menu"))
                    {
                        // Lógica de menus (simplificada para este bloque, similar a tu original)
                        // ... (Mantenemos tu lógica de parseo de menús aquí si la necesitas, 
                        // pero la clave es el parseo de controles abajo)
                        var parts = trimmedLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        // Calculamos el nivel basándonos en la indentación (espacios iniciales)
                        // En VB6, cada nivel de profundidad suele tener 3 o 6 espacios de diferencia.
                        int leadingSpaces = line.TakeWhile(char.IsWhiteSpace).Count();
                        int level = leadingSpaces > 0 ? (leadingSpaces / 3) - 1 : 0;
                        if (level < 0) level = 0;

                        var menu = new MenuModel
                        {
                            Name = parts.Length > 2 ? parts[2] : "mnuUnknown",
                            Level = level
                        };

                        // Leer propiedades del menú hasta encontrar su "End"
                        while ((line = reader.ReadLine()) != null)
                        {
                            string mLine = line.Trim();
                            if (mLine == "End") break;

                            if (mLine.Contains("="))
                            {
                                var p = mLine.Split(new char[] { '=' }, 2);
                                string key = p[0].Trim();
                                string val = p[1].Trim().Replace("\"", "");

                                // Quitar comentarios al final de la línea si existen
                                if (val.Contains("'")) val = val.Split('\'')[0].Trim();

                                switch (key)
                                {
                                    case "Caption": menu.Caption = val; break;
                                    case "Shortcut": menu.Shortcut = val; break;
                                    case "Checked": menu.Checked = val == "-1" || val.ToLower() == "true"; break;
                                    case "Enabled": menu.Enabled = val != "0" && val.ToLower() != "false"; break;
                                    case "Visible": menu.Visible = val != "0" && val.ToLower() != "false"; break;
                                    case "Index":
                                        if (int.TryParse(val, out int idx)) /* logic for array if needed */;
                                        break;
                                }
                            }
                        }
                        tempMenuList.Add(menu);
                        continue;
                    }

                    // 2. INICIO CONTROL
                    if (trimmedLine.StartsWith("Begin ") && !trimmedLine.StartsWith("BeginProperty"))
                    {
                        var parts = trimmedLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            var newCtrl = new VbControlModel
                            {
                                Type = parts[1],
                                Name = parts.Length > 2 ? parts[2] : "Unknown"
                            };

                            if (stack.Count > 0) stack.Peek().Children.Add(newCtrl);
                            else root = newCtrl;

                            stack.Push(newCtrl);
                        }
                    }
                    // 3. FIN CONTROL
                    else if (trimmedLine == "End")
                    {
                        if (stack.Count > 0) stack.Pop();
                    }
                    // 4. FUENTES
                    else if (trimmedLine.StartsWith("BeginProperty Font"))
                    {
                        if (stack.Count > 0)
                        {
                            var fontProps = ReadPropertyBlock(reader);
                            stack.Peek().Properties["Font"] = BuildFontString(fontProps);
                        }
                    }
                    // 5. PROPIEDADES
                    else if (trimmedLine.Contains("="))
                    {
                        var parts = trimmedLine.Split(new char[] { '=' }, 2);
                        if (parts.Length == 2 && stack.Count > 0)
                        {
                            string key = parts[0].Trim();
                            string val = parts[1].Trim();
                            if (val.Contains("'")) val = val.Split('\'')[0].Trim();
                            if (val.StartsWith("\"") && val.EndsWith("\"")) val = val.Substring(1, val.Length - 2);

                            stack.Peek().Properties[key] = val;
                        }
                    }
                }
                if (root != null)
                {
                    root.Menus = tempMenuList;
                }
                return root;
            }
        }

        private static Dictionary<string, string> ReadPropertyBlock(StringReader reader)
        {
            var props = new Dictionary<string, string>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line == "EndProperty") break;
                if (line.Contains("="))
                {
                    var p = line.Split('=');
                    props[p[0].Trim()] = p[1].Trim().Replace("\"", "");
                }
            }
            return props;
        }

        /// <summary>
        /// Builds a font string from a dictionary of font properties.
        /// </summary>
        private static string BuildFontString(Dictionary<string, string> fontProps)
        {
            string name = fontProps.ContainsKey("Name") ? fontProps["Name"] : "Microsoft Sans Serif";
            string size = fontProps.ContainsKey("Size") ? fontProps["Size"] : "8.25";
            List<string> styles = new List<string>();

            if (fontProps.ContainsKey("Weight") && double.TryParse(fontProps["Weight"], out double w) && w > 400) styles.Add("Bold");
            if (fontProps.ContainsKey("Italic") && (fontProps["Italic"] == "-1")) styles.Add("Italic");
            if (fontProps.ContainsKey("Underline") && (fontProps["Underline"] == "-1" || fontProps["Underline"].ToLower() == "true")) styles.Add("Underline");
            if (fontProps.ContainsKey("Strikethrough") && (fontProps["Strikethrough"] == "-1" || fontProps["Strikethrough"].ToLower() == "true")) styles.Add("Strikethrough");


            string stylePart = styles.Count > 0 ? "; " + string.Join("; ", styles) : "";
            return $"{name}; {size}pt{stylePart}";
        }

        /// <summary>
        /// Converts twips measurement to pixels.
        /// </summary>
        /// <param name="twipsVal">The twips value as a string.</param>
        /// <returns>The equivalent value in pixels.</returns>
        public static double TwipsToPixels(string twipsVal)
        {
            if (double.TryParse(twipsVal, NumberStyles.Any, CultureInfo.InvariantCulture, out double t)) return t / TwipsPerPixel;
            return 0;
        }

        /// <summary>
        /// Converts pixels to twips measurement.
        /// </summary>
        /// <param name="pixels">The pixel value.</param>
        /// <returns>The equivalent value in twips.</returns>
        public static long PixelsToTwips(double pixels)
        {
            if (double.IsNaN(pixels)) return 0;
            return (long)Math.Round(pixels * TwipsPerPixel);
        }

        /// <summary>
        /// Gets a property value from a VB control model.
        /// </summary>
        /// <param name="model">The control model.</param>
        /// <param name="propName">The property name.</param>
        /// <returns>The property value or "0" if not found.</returns>
        public static string GetPropVal(VbControlModel model, string propName)
        {
            return model.Properties.ContainsKey(propName) ? model.Properties[propName] : "0";
        }
    }
}