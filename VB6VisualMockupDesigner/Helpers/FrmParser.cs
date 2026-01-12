using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VB6VisualMockupDesigner.Controls; // Asegúrate de tener tus propios namespaces
using VB6VisualMockupDesigner.Models;

namespace VB6VisualMockupDesigner.Helpers
{
    public static class FrmParser
    {
        private const double TwipsPerPixel = 15.0;

        public static void Parse(string filePath, DesignerCanvas canvas)
        {
            if (!File.Exists(filePath)) return;

            canvas.ClearCanvas();
            string[] lines = File.ReadAllLines(filePath);

            // Pila para la jerarquía visual
            Stack<FrameworkElement> contextStack = new Stack<FrameworkElement>();

            // Regex mejorado para capturar tipos complejos (ej: COBISMap60.Map32)
            Regex beginRegex = new Regex(@"^\s*Begin\s+([\w\.]+)\s+(\w+)?");

            // Regex para propiedades
            Regex propRegex = new Regex(@"^\s*(\w+)\s*=\s*(.*)");

            foreach (string line in lines)
            {
                string cleanLine = line.Trim();
                if (string.IsNullOrWhiteSpace(cleanLine)) continue;

                // 1. DETECTAR INICIO DE BLOQUE
                Match beginMatch = beginRegex.Match(cleanLine);
                if (beginMatch.Success)
                {
                    string fullType = beginMatch.Groups[1].Value; // Ej: VB.CommandButton o Threed.SSPanel
                    string name = beginMatch.Groups[2].Value;     // Ej: cmdOK

                    // Extraemos solo el tipo final (quitamos librería)
                    string vbType = fullType.Contains(".") ? fullType.Split('.')[1] : fullType;

                    // CASO ESPECIAL 1: EL FORMULARIO (Normal o MDI)
                    if (vbType == "Form" || vbType == "MDIForm")
                    {
                        contextStack.Push(null); // Null representa la Raíz
                        // Podrías guardar en el canvas que es tipo MDI si quisieras
                    }
                    // CASO ESPECIAL 2: MENUS
                    else if (vbType == "Menu")
                    {
                        // Los menús en VB6 no tienen coordenadas visuales en el canvas.
                        // Por ahora, creamos un placeholder invisible o simplemente un objeto dummy 
                        // para mantener la integridad del Stack (Begin/End), pero NO lo agregamos al Canvas.
                        var menuPlaceholder = new Control { Visibility = Visibility.Collapsed, Name = "Menu_Ignore" };
                        contextStack.Push(menuPlaceholder);
                    }
                    // CONTROLES NORMALES
                    else
                    {
                        var control = canvas.CreateRetroControl(vbType) as FrameworkElement;

                        if (control != null)
                        {
                            if (!string.IsNullOrEmpty(name))
                            {
                                control.Name = CleanName(name); // Limpieza de nombre para WPF
                                control.Tag = name; // Guardamos nombre original
                            }

                            // Lógica de Contenedores (Si el padre es un contenedor visual)
                            // Nota: En un Canvas plano, todo se agrega al canvas. 
                            // Si quisieras anidar visualmente en WPF (Grid dentro de Grid), aquí cambiaría la lógica.
                            // Por ahora mantenemos la lógica plana del MockupDesigner visual:

                            // Solo agregamos al canvas si NO es un menú oculto
                            if (contextStack.Count > 0 && contextStack.Peek()?.Name == "Menu_Ignore")
                            {
                                // Es un submenú, lo ignoramos visualmente
                            }
                            else
                            {
                                canvas.AddControlToCanvas(control, 0, 0);
                            }

                            contextStack.Push(control);
                        }
                        else
                        {
                            // Control desconocido
                            contextStack.Push(new Control { Visibility = Visibility.Collapsed });
                        }
                    }
                    continue;
                }

                // 2. DETECTAR FIN DE BLOQUE
                if (cleanLine.StartsWith("End", StringComparison.OrdinalIgnoreCase))
                {
                    if (cleanLine.Length == 3 || char.IsWhiteSpace(cleanLine[3])) // Asegura que es "End" y no "EndProperty"
                    {
                        if (contextStack.Count > 0) contextStack.Pop();
                        continue;
                    }
                }

                // 3. LEER PROPIEDADES
                Match propMatch = propRegex.Match(cleanLine);
                if (propMatch.Success && contextStack.Count > 0)
                {
                    string propName = propMatch.Groups[1].Value;
                    string propValue = propMatch.Groups[2].Value.Trim('"');

                    var currentObj = contextStack.Peek();

                    // Si es un objeto ignorado (como Menú), saltamos propiedades
                    if (currentObj != null && currentObj.Name == "Menu_Ignore") continue;

                    if (currentObj == null)
                    {
                        ApplyFormProperty(canvas, propName, propValue);
                    }
                    else
                    {
                        ApplyControlProperty(currentObj, propName, propValue);
                    }
                }
            }
        }

        private static string CleanName(string name)
        {
            // WPF x:Name no permite ciertos caracteres que VB6 a veces toleraba o arrays
            return System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
        }

        private static void ApplyFormProperty(DesignerCanvas canvas, string prop, string val)
        {
            if (double.TryParse(val, out double numVal))
            {
                double pixels = numVal / TwipsPerPixel;
                switch (prop)
                {
                    case "ClientWidth":
                        canvas.SetFormDimensions(pixels, canvas.ActualHeight);
                        break;
                    case "ClientHeight":
                        canvas.SetFormDimensions(canvas.Width > 0 ? canvas.Width : 800, pixels);
                        break;
                }
            }

            if (prop == "Caption") canvas.FormTitle = val;

            // Detectar si es MDI por el color de fondo típico
            if (prop == "BackColor" && val.Contains("&H8000000C"))
            {
                // Es el color "App Workspace", típico de MDI
                // Podrías cambiar el color de fondo del canvas aquí
                // canvas.Background = Brushes.DarkGray; 
            }
        }

        private static void ApplyControlProperty(FrameworkElement control, string prop, string val)
        {
            // PROPIEDADES NUMÉRICAS
            if (double.TryParse(val, out double numVal))
            {
                double pixels = numVal / TwipsPerPixel;

                switch (prop)
                {
                    case "Left": Canvas.SetLeft(control, pixels); break;
                    case "Top": Canvas.SetTop(control, pixels); break;
                    case "Width": control.Width = pixels; break;
                    case "Height": control.Height = pixels; break;
                }
            }

            // PROPIEDADES ESPECIALES
            switch (prop)
            {
                case "Caption":
                    if (control is ContentControl cc) cc.Content = val;
                    if (control is TextBlock lbl) lbl.Text = val;
                    break;
                case "Text":
                    if (control is TextBox tb) tb.Text = val;
                    break;
                case "Tag":
                    control.Tag = val;
                    break;
                case "Align":
                    // VB6 Align: 1=Top, 2=Bottom, 3=Left, 4=Right
                    // Esto es complejo en un Canvas, pero podemos simularlo
                    // o guardarlo en el Tag para procesarlo luego.
                    if (val == "1") // Top
                    {
                        Canvas.SetTop(control, 0);
                        Canvas.SetLeft(control, 0);
                        control.Width = double.NaN; // Stretch horizontal (simulado)
                        // En un Canvas real, el stretch no funciona automático sin binding, 
                        // pero al menos lo ponemos arriba.
                    }
                    else if (val == "2") // Bottom
                    {
                        // Difícil saber el Bottom exacto sin saber el alto del form en este momento
                        // Lo marcamos para referencia futura
                        control.Tag = "Align:Bottom";
                    }
                    break;
            }
        }
    }
}