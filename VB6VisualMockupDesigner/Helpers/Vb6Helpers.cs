using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VB6VisualMockupDesigner.Helpers
{
    // ==========================================
    // 1. EL MODELO DE DATOS (ÁRBOL)
    // ==========================================
    public class VbControlModel
    {
        public string Type { get; set; }        // Ej: VB.CommandButton
        public string Name { get; set; }        // Ej: cmdAceptar

        // Diccionario para propiedades (Left, Top, Caption, etc.)
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        // Lista de hijos (Aquí vive la jerarquía)
        public List<VbControlModel> Children { get; set; } = new List<VbControlModel>();
    }

    // ==========================================
    // 2. EL PARSER (CEREBRO DE CARGA)
    // ==========================================
    public static class Vb6Helpers
    {
        /// <summary>
        /// Lee el texto crudo de un .frm y devuelve el objeto raíz (Form) con todos sus hijos anidados.
        /// </summary>
        public static VbControlModel ParseVb6Form(string fileContent)
        {
            // 1. Normalizar saltos de línea y limpiar vacíos
            var lines = fileContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // 2. Pila para rastrear la profundidad (Quién es el padre actual)
            Stack<VbControlModel> stack = new Stack<VbControlModel>();

            VbControlModel rootForm = null;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // ---------------------------------------------------------
                // A. INICIO DE BLOQUE: "Begin VB.Control Nombre"
                // ---------------------------------------------------------
                if (line.StartsWith("Begin "))
                {
                    // Separar partes: [0]Begin [1]Tipo [2]Nombre
                    var parts = line.Split(' ');

                    if (parts.Length >= 2)
                    {
                        string type = parts[1];
                        string name = parts.Length > 2 ? parts[2] : "Unknown";

                        var newControl = new VbControlModel { Type = type, Name = name };

                        if (stack.Count == 0)
                        {
                            // Si la pila está vacía, este es el Formulario (Raíz)
                            rootForm = newControl;
                        }
                        else
                        {
                            // Si hay pila, el tope es mi PADRE. Me agrego a sus hijos.
                            var parent = stack.Peek();
                            parent.Children.Add(newControl);
                        }

                        // Me subo a la pila. Ahora yo soy el "Padre Activo" para lo que siga.
                        stack.Push(newControl);
                    }
                }
                // ---------------------------------------------------------
                // B. FIN DE BLOQUE: "End"
                // ---------------------------------------------------------
                else if (line.StartsWith("End"))
                {
                    if (stack.Count > 0)
                    {
                        // Ya terminé de leer este control y sus hijos. Me bajo de la pila.
                        stack.Pop();
                    }
                }
                // ---------------------------------------------------------
                // C. PROPIEDADES: "Caption = 'Hola Mundo'"
                // ---------------------------------------------------------
                else if (line.Contains("=") && stack.Count > 0 && !line.StartsWith("Attribute"))
                {
                    var currentControl = stack.Peek(); // Propiedad del control actual en el tope

                    // Dividimos solo en el PRIMER igual (para soportar textos con '=')
                    var parts = line.Split(new[] { '=' }, 2);

                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        // Limpieza: Quitar comentarios (') al final, si no están entre comillas
                        if (value.Contains("'") && !value.StartsWith("\""))
                        {
                            value = value.Split('\'')[0].Trim();
                        }

                        // Limpieza: Quitar comillas de strings
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        // Guardar
                        if (!currentControl.Properties.ContainsKey(key))
                        {
                            currentControl.Properties[key] = value;
                        }
                    }
                }
            }

            return rootForm;
        }

        // ==========================================
        // 3. UTILIDADES DE CONVERSIÓN
        // ==========================================

        // Conversión estándar: 15 Twips = 1 Pixel (a 96 DPI)
        public static double TwipsToPixels(string twipsStr)
        {
            if (string.IsNullOrWhiteSpace(twipsStr)) return 0;
            if (double.TryParse(twipsStr, out double val)) return val / 15.0;
            return 0;
        }

        // Sobrecarga para doubles directos
        public static double TwipsToPixels(double twips) => twips / 15.0;

        // Helper seguro para obtener propiedades del diccionario (evita KeyNotFoundException)
        public static string GetPropVal(VbControlModel model, string key)
        {
            if (model.Properties.TryGetValue(key, out string val)) return val;
            return "0"; // Valor por defecto seguro para cálculos matemáticos
        }
    }
}