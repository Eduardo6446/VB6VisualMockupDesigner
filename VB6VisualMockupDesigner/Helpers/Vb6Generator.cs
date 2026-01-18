using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes; // Para Rectangle/Path
using VB6VisualMockupDesigner.Helpers;
using VB6VisualMockupDesigner.Controls; // Para acceder a VB6Data (Indices)

namespace VB6VisualMockupDesigner.Helpers
{
    public static class Vb6Generator
    {
        private const double TwipsPerPixel = 15.0;

        public static string GenerateFrmCode(Canvas designSurface, string formTitle)
        {
            string vbName = SanitizeVbName(formTitle);
            StringBuilder sb = new StringBuilder();

            // Cabecera
            sb.AppendLine("VERSION 5.00");
            sb.AppendLine($"' Created with {VersionInfo.AppName} v{VersionInfo.FullVersion}");
            sb.AppendLine($"' Date: {DateTime.Now}");

            sb.AppendLine($"Begin VB.Form {vbName} ");

            // Propiedades del Form
            sb.AppendLine($"   Caption         =   \"{formTitle}\"");
            sb.AppendLine($"   ClientHeight    =   {Math.Round(designSurface.ActualHeight * TwipsPerPixel)}");
            sb.AppendLine($"   ClientWidth     =   {Math.Round(designSurface.ActualWidth * TwipsPerPixel)}");
            sb.AppendLine("   ScaleHeight     =   " + Math.Round(designSurface.ActualHeight * TwipsPerPixel));
            sb.AppendLine("   ScaleWidth      =   " + Math.Round(designSurface.ActualWidth * TwipsPerPixel));
            sb.AppendLine("   StartUpPosition =   3  'Windows Default");

            // Escribir hijos
            WriteChildrenRecursively(designSurface, sb, 3);

            sb.AppendLine("End");

            // Atributos ocultos
            sb.AppendLine($"Attribute VB_Name = \"{vbName}\"");
            sb.AppendLine("Attribute VB_GlobalNameSpace = False");
            sb.AppendLine("Attribute VB_Creatable = False");
            sb.AppendLine("Attribute VB_PredeclaredId = True");
            sb.AppendLine("Attribute VB_Exposed = False");

            return sb.ToString();
        }

        private static void WriteChildrenRecursively(Panel container, StringBuilder sb, int indentLevel)
        {
            string indent = new string(' ', indentLevel);

            foreach (UIElement child in container.Children)
            {
                if (child is FrameworkElement fe)
                {
                    // =========================================================
                    // 🛑 ZONA DE FILTROS: Evitar guardar "basura" visual
                    // =========================================================

                    // 1. Ignorar utilidades por Nombre
                    if (fe.Name == "SelectionRect" ||
                        fe.Name == "QuickEditBox" ||
                        fe.Name == "SnapLineOverlay" ||
                        fe.Name == "InfoTip") // <--- Aquí ignoramos el tooltip de coordenadas
                        continue;

                    // 2. Ignorar Handles de Redimensión (Los 8 puntos)
                    // Verificamos si el Tag es del tipo Enum ResizeDirection
                    if (fe.Tag is ResizeDirection) continue;

                    // 3. Ignorar Adornos genéricos (Rectángulos/Bordes sin Tag y sin HitTest)
                    // Esto atrapa los bordes azules de selección
                    if ((fe is Rectangle || fe is Border) && fe.Tag == null && !fe.IsHitTestVisible) continue;

                    // 4. Ignorar Fantasmas de arrastre
                    if (fe.GetType().Name == "DragAdorner") continue;

                    // =========================================================

                    // Si pasa el filtro, es un Control Real
                    string vbType = fe.Tag?.ToString() ?? "VB.Unknown";
                    string name = string.IsNullOrEmpty(fe.Name) ? "Control" : fe.Name;

                    // Inicio del bloque
                    sb.AppendLine($"{indent}Begin {vbType} {name} ");

                    // Coordenadas
                    double left = Canvas.GetLeft(fe);
                    double top = Canvas.GetTop(fe);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;

                    sb.AppendLine($"{indent}   Left            =   {Math.Round(left * TwipsPerPixel)}");
                    sb.AppendLine($"{indent}   Top             =   {Math.Round(top * TwipsPerPixel)}");
                    sb.AppendLine($"{indent}   Width           =   {Math.Round(fe.ActualWidth * TwipsPerPixel)}");
                    sb.AppendLine($"{indent}   Height          =   {Math.Round(fe.ActualHeight * TwipsPerPixel)}");

                    // Propiedades
                    if (fe is ContentControl cc && cc.Content is string cap)
                        sb.AppendLine($"{indent}   Caption         =   \"{cap}\"");
                    else if (fe is TextBox tb)
                        sb.AppendLine($"{indent}   Text            =   \"{tb.Text}\"");
                    else if (fe is TextBlock txt)
                        sb.AppendLine($"{indent}   Caption         =   \"{txt.Text}\"");

                    // Index
                    int? idx = VB6Data.GetIndex(fe);
                    if (idx.HasValue)
                        sb.AppendLine($"{indent}   Index           =   {idx.Value}");

                    // Recursividad (Hijos dentro de contenedores)
                    Canvas innerCanvas = GetChildCanvas(fe);
                    if (innerCanvas != null && innerCanvas.Children.Count > 0)
                    {
                        WriteChildrenRecursively(innerCanvas, sb, indentLevel + 3);
                    }

                    sb.AppendLine($"{indent}End");
                }
            }
        }

        private static string SanitizeVbName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Form1";
            string clean = input.Replace(" ", "");
            char[] arr = clean.ToCharArray();
            clean = new string(Array.FindAll(arr, c => char.IsLetterOrDigit(c) || c == '_'));
            if (clean.Length > 0 && char.IsDigit(clean[0])) clean = "frm" + clean;
            return string.IsNullOrEmpty(clean) ? "Form1" : clean;
        }

        private static Canvas GetChildCanvas(FrameworkElement container)
        {
            if (container is GroupBox gb) return gb.Content as Canvas;
            if (container is Border b && b.Child is Canvas c) return c;
            if (container is Border bp && bp.Child is Grid g)
            {
                foreach (var k in g.Children) if (k is Canvas c2) return c2;
            }
            return null;
        }
    }
}