using System;
using System.Collections.Generic;
using System.Linq;

namespace VB6VisualMockupDesigner.Helpers
{
    // MODELO DE DATOS
    public class VbControlModel
    {
        public string Type { get; set; }        // Ej: VB.PictureBox
        public string Name { get; set; }        // Ej: picBoxMain
        public int Index { get; set; } = -1;    // Para arrays
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public List<VbControlModel> Children { get; set; } = new List<VbControlModel>();
        public VbControlModel Parent { get; set; }
    }

    // LÓGICA ESTÁTICA (Parser y Conversor)
    public static class Vb6Helpers
    {
        // 1 pixel ≈ 15 twips
        public static double TwipsToPixels(double twips) => twips / 15.0;

        public static double TwipsToPixels(string twipsStr)
        {
            if (double.TryParse(twipsStr, out double d)) return d / 15.0;
            return 0;
        }

        public static double GetPropVal(VbControlModel m, string key)
        {
            if (m.Properties.ContainsKey(key) && double.TryParse(m.Properties[key], out double val))
                return val;
            return 0;
        }

        public static VbControlModel ParseVb6Form(string fileContent)
        {
            var lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            VbControlModel root = null;
            VbControlModel current = null;
            Stack<VbControlModel> stack = new Stack<VbControlModel>();

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("Begin "))
                {
                    var parts = line.Substring(6).Split(' ');
                    string type = parts[0];
                    string name = parts.Length > 1 ? parts[1] : type;

                    var newControl = new VbControlModel { Type = type, Name = name, Parent = current };

                    if (current != null) current.Children.Add(newControl);
                    else root = newControl;

                    current = newControl;
                    stack.Push(current);
                }
                else if (line == "End")
                {
                    if (stack.Count > 0)
                    {
                        stack.Pop();
                        current = stack.Count > 0 ? stack.Peek() : null;
                    }
                }
                else if (line.Contains("=") && current != null)
                {
                    var eqIndex = line.IndexOf('=');
                    string propName = line.Substring(0, eqIndex).Trim();
                    string propValue = line.Substring(eqIndex + 1).Trim();

                    if (propValue.Contains("'")) propValue = propValue.Substring(0, propValue.IndexOf("'")).Trim();
                    propValue = propValue.Replace("\"", "");

                    if (!current.Properties.ContainsKey(propName))
                        current.Properties.Add(propName, propValue);
                }
            }
            return root;
        }
    }
}