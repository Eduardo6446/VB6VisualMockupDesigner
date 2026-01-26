using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using VB6VisualMockupDesigner.Models;

namespace VB6VisualMockupDesigner.Helpers
{
    public static class Vb6Helpers
    {
        private const double TwipsPerPixel = 15.0;

        public static VbControlModel ParseVb6Form(string fileContent)
        {
            using (StringReader reader = new StringReader(fileContent))
            {
                string line;
                VbControlModel root = null;
                Stack<VbControlModel> stack = new Stack<VbControlModel>();

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("'") || line.StartsWith("Attribute") || line.StartsWith("VERSION")) continue;

                    // 1. INICIO DE CONTROL (Evitamos confundirnos con BeginProperty)
                    if (line.StartsWith("Begin ") && !line.StartsWith("BeginProperty"))
                    {
                        var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
                    // 2. FIN DE CONTROL
                    else if (line == "End")
                    {
                        if (stack.Count > 0) stack.Pop();
                    }
                    // =========================================================
                    // 3. BLOQUE DE FUENTE (ESTO ES LO QUE TE FALTA)
                    // =========================================================
                    else if (line.StartsWith("BeginProperty Font"))
                    {
                        if (stack.Count > 0)
                        {

                            // Leemos las líneas internas del bloque
                            var fontProps = ReadPropertyBlock(reader);

                            // Creamos el string "file:///...; 12pt; Bold"
                            string compiledFont = BuildFontString(fontProps);

                            // Lo inyectamos en el control
                            stack.Peek().Properties["Font"] = compiledFont;
                        }
                    }
                    // =========================================================

                    // 4. PROPIEDADES NORMALES
                    else if (line.Contains("="))
                    {
                        var parts = line.Split(new char[] { '=' }, 2);
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
                return root;
            }
        }

        // --- HELPER CRÍTICO QUE TE FALTA ---
        private static Dictionary<string, string> ReadPropertyBlock(StringReader reader)
        {
            var props = new Dictionary<string, string>();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line == "EndProperty") break; // Salir al terminar bloque

                if (line.Contains("="))
                {
                    var parts = line.Split(new char[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string val = parts[1].Trim();

                        if (val.Contains("'")) val = val.Split('\'')[0].Trim();
                        if (val.StartsWith("\"") && val.EndsWith("\"")) val = val.Substring(1, val.Length - 2);

                        props[key] = val;
                    }
                }
            }
            return props;
        }

        // --- CONSTRUCTOR DE STRING DE FUENTE ---
        private static string BuildFontString(Dictionary<string, string> fontProps)
        {
            string name = "Microsoft Sans Serif";
            string size = "8.25";
            List<string> styles = new List<string>();

            if (fontProps.ContainsKey("Name")) name = fontProps["Name"];
            if (fontProps.ContainsKey("Size")) size = fontProps["Size"];

            if (fontProps.ContainsKey("Weight") && double.TryParse(fontProps["Weight"], out double w) && w > 400) styles.Add("Bold");
            if (fontProps.ContainsKey("Italic") && (fontProps["Italic"] == "-1" || fontProps["Italic"].ToLower() == "true")) styles.Add("Italic");
            if (fontProps.ContainsKey("Underline") && (fontProps["Underline"] == "-1" || fontProps["Underline"].ToLower() == "true")) styles.Add("Underline");
            if (fontProps.ContainsKey("Strikethrough") && (fontProps["Strikethrough"] == "-1" || fontProps["Strikethrough"].ToLower() == "true")) styles.Add("Strikethrough");

            string stylePart = styles.Count > 0 ? "; " + string.Join("; ", styles) : "";
            return $"{name}; {size}pt{stylePart}";
        }

        public static double TwipsToPixels(string twipsVal)
        {
            if (double.TryParse(twipsVal, NumberStyles.Any, CultureInfo.InvariantCulture, out double t)) return t / TwipsPerPixel;
            return 0;
        }

        public static long PixelsToTwips(double pixels)
        {
            if (double.IsNaN(pixels)) return 0;
            return (long)Math.Round(pixels * TwipsPerPixel);
        }

        public static string GetPropVal(VbControlModel model, string propName)
        {
            return model.Properties.ContainsKey(propName) ? model.Properties[propName] : "0";
        }
    }
}