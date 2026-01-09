using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using VB6VisualMockupDesigner.Views; // Requiere .NET Core 3.1 o superior (o NuGet en .NET Framework)
using VB6VisualMockupDesigner.Models;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Services;

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

            // Pila para saber en qué objeto estamos (Formulario o Control)
            Stack<FrameworkElement> contextStack = new Stack<FrameworkElement>();

            // Regex para detectar inicios: "Begin VB.CommandButton cmdOK"
            Regex beginRegex = new Regex(@"^\s*Begin\s+VB\.(\w+)\s+(\w+)?");

            // Regex para propiedades: "Caption = "Hola Mundo"" o "Left = 1200"
            Regex propRegex = new Regex(@"^\s*(\w+)\s*=\s*(.*)");

            foreach (string line in lines)
            {
                string cleanLine = line.Trim();
                if (string.IsNullOrWhiteSpace(cleanLine)) continue;

                // 1. DETECTAR INICIO DE BLOQUE
                Match beginMatch = beginRegex.Match(cleanLine);
                if (beginMatch.Success)
                {
                    string vbType = beginMatch.Groups[1].Value; // Ej: CommandButton
                    string name = beginMatch.Groups[2].Value;   // Ej: cmdOK

                    if (vbType == "Form")
                    {
                        // Es el formulario raíz
                        contextStack.Push(null); // Usamos null para representar "El Formulario"
                    }
                    else
                    {
                        // Es un control
                        var control = canvas.CreateRetroControl(vbType) as FrameworkElement;
                        if (control != null)
                        {
                            // Guardamos el nombre (podrías asignarlo a Tag o Name si limpias caracteres raros)
                            if (!string.IsNullOrEmpty(name)) control.Tag = name;

                            // Lo añadimos al canvas (posición 0,0 temporalmente)
                            canvas.AddControlToCanvas(control, 0, 0);

                            // Lo apilamos para leer sus propiedades
                            contextStack.Push(control);
                        }
                        else
                        {
                            // Control desconocido, apilamos un placeholder para no romper la estructura
                            contextStack.Push(new Control());
                        }
                    }
                    continue;
                }

                // 2. DETECTAR FIN DE BLOQUE
                if (cleanLine.Equals("End", StringComparison.OrdinalIgnoreCase))
                {
                    if (contextStack.Count > 0) contextStack.Pop();
                    continue;
                }

                // 3. LEER PROPIEDADES
                Match propMatch = propRegex.Match(cleanLine);
                if (propMatch.Success && contextStack.Count > 0)
                {
                    string propName = propMatch.Groups[1].Value;
                    string propValue = propMatch.Groups[2].Value.Trim('"'); // Quitamos comillas

                    var currentObj = contextStack.Peek();

                    if (currentObj == null)
                    {
                        // ESTAMOS EN EL FORMULARIO
                        ApplyFormProperty(canvas, propName, propValue);
                    }
                    else
                    {
                        // ESTAMOS EN UN CONTROL
                        ApplyControlProperty(currentObj, propName, propValue);
                    }
                }
            }
        }

        private static void ApplyFormProperty(DesignerCanvas canvas, string prop, string val)
        {
            // VB6 guarda ClientHeight/ClientWidth en Twips
            if (double.TryParse(val, out double numVal))
            {
                double pixels = numVal / TwipsPerPixel;

                switch (prop)
                {
                    case "ClientWidth":
                        // Obtenemos alto actual para no perderlo
                        canvas.SetFormDimensions(pixels, canvas.ActualHeight);
                        break;
                    case "ClientHeight":
                        // Solo cambiamos alto
                        // Nota: Idealmente canvas.SetFormDimensions debería permitir cambiar uno solo
                        // pero por simplicidad asumimos que vendrán en orden o ajustamos luego.
                        // Hack temporal: Accedemos al ancho actual del contenedor si es posible, o usamos default
                        canvas.SetFormDimensions(canvas.Width > 0 ? canvas.Width : 480, pixels);
                        break;
                }
            }

            if (prop == "Caption")
            {
                canvas.FormTitle = val;
            }
        }

        private static void ApplyControlProperty(FrameworkElement control, string prop, string val)
        {
            // PROPIEDADES NUMÉRICAS (Coordenadas y Tamaño)
            if (double.TryParse(val, out double numVal))
            {
                double pixels = numVal / TwipsPerPixel;

                switch (prop)
                {
                    case "Left":
                        Canvas.SetLeft(control, pixels);
                        break;
                    case "Top":
                        Canvas.SetTop(control, pixels);
                        break;
                    case "Width":
                        control.Width = pixels;
                        break;
                    case "Height":
                        control.Height = pixels;
                        break;
                }
            }

            // PROPIEDADES DE TEXTO
            switch (prop)
            {
                case "Caption":
                    if (control is ContentControl cc) cc.Content = val;
                    break;
                case "Text":
                    if (control is TextBox tb) tb.Text = val;
                    break;
                case "Tag": // VB6 Tag
                    control.Tag = val;
                    break;
            }
        }
    }
}
