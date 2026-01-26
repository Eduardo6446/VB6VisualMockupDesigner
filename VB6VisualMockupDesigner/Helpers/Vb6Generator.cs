using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VB6VisualMockupDesigner.Controls; // Necesario para VB6Data y ResizeDirection
using VB6VisualMockupDesigner.Helpers;

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
            sb.AppendLine($"' Date: {DateTime.Now}");
            sb.AppendLine($"Begin VB.Form {vbName} ");

            // Propiedades del Form
            sb.AppendLine($"   Caption         =   \"{formTitle}\"");
            sb.AppendLine($"   ClientHeight    =   {Math.Round(designSurface.ActualHeight * TwipsPerPixel)}");
            sb.AppendLine($"   ClientWidth     =   {Math.Round(designSurface.ActualWidth * TwipsPerPixel)}");
            sb.AppendLine($"   ScaleHeight     =   {Math.Round(designSurface.ActualHeight * TwipsPerPixel)}");
            sb.AppendLine($"   ScaleWidth      =   {Math.Round(designSurface.ActualWidth * TwipsPerPixel)}");
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
                    // 🛑 FILTROS DE SEGURIDAD (ANTI-BASURA)
                    // =========================================================

                    // 1. Ignorar elementos de sistema por Nombre
                    if (fe.Name == "SelectionRect" ||
                        fe.Name == "QuickEditBox" ||
                        fe.Name == "SnapLineOverlay" ||
                        fe.Name == "InfoTip")
                        continue;

                    // 2. Ignorar Handles de Redimensión (Los cuadritos blancos)
                    // Estos suelen tener un Tag del tipo Enum "ResizeDirection"
                    if (fe.Tag is ResizeDirection) continue;

                    // 3. Ignorar Bordes de Selección (Adorners azules)
                    // Son Borders sin Tag y que no reciben clics
                    if (fe is Border && fe.Tag == null && !fe.IsHitTestVisible) continue;

                    // 4. Ignorar cualquier Rectangle o Ellipse que no tenga Tag explícito
                    // (Los controles Shape de VB6 sí tendrán Tag="Shape")
                    if ((fe is Rectangle || fe is Ellipse || fe is Path) && fe.Tag == null) continue;

                    // 5. Ignorar el Grid Fantasma de arrastre (si existe)
                    if (fe.GetType().Name.Contains("Adorner")) continue;

                    // =========================================================

                    // Obtener Tipo VB6
                    string vbType = GetVbType(fe);

                    // Si devuelve Unknown, es algo que no reconocemos -> NO GUARDAR
                    if (vbType == "VB.Unknown") continue;

                    string name = string.IsNullOrEmpty(fe.Name) ? "Control" : fe.Name;

                    // --- INICIO DEL CONTROL ---
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

                    // Propiedades Visuales (Fuentes y Colores)
                    if (fe is Control c)
                    {
                        WriteFontBlock(sb, c, indent + "   ");

                        if (c.Background is SolidColorBrush bg)
                            sb.AppendLine($"{indent}   BackColor       =   {ColorToVbHex(bg.Color)}");

                        if (c.Foreground is SolidColorBrush fg)
                            sb.AppendLine($"{indent}   ForeColor       =   {ColorToVbHex(fg.Color)}");
                    }
                    else if (fe is Border b) // Para PictureBox o Frames simulados con Border
                    {
                        if (b.Background is SolidColorBrush bg)
                            sb.AppendLine($"{indent}   BackColor       =   {ColorToVbHex(bg.Color)}");
                    }

                    // Propiedades de Contenido
                    if (fe is ContentControl cc && cc.Content is string cap)
                        sb.AppendLine($"{indent}   Caption         =   \"{cap}\"");
                    else if (fe is TextBox tb)
                        sb.AppendLine($"{indent}   Text            =   \"{tb.Text}\"");
                    else if (fe is TextBlock txt)
                        sb.AppendLine($"{indent}   Caption         =   \"{txt.Text}\"");

                    // Index (Arrays)
                    int? idx = VB6Data.GetIndex(fe);
                    if (idx.HasValue)
                        sb.AppendLine($"{indent}   Index           =   {idx.Value}");

                    // Recursividad (Para Frames/PictureBoxes que tienen hijos)
                    Canvas innerCanvas = GetChildCanvas(fe);
                    if (innerCanvas != null && innerCanvas.Children.Count > 0)
                    {
                        WriteChildrenRecursively(innerCanvas, sb, indentLevel + 3);
                    }

                    sb.AppendLine($"{indent}End");
                }
            }
        }

        private static void WriteFontBlock(StringBuilder sb, Control ctrl, string localIndent)
        {
            var ff = ctrl.FontFamily;
            var fs = ctrl.FontSize;
            var fw = ctrl.FontWeight;
            var fstyle = ctrl.FontStyle;

            sb.AppendLine($"{localIndent}BeginProperty Font ");

            // Guardamos la ruta completa (file:///...) si es importada, o el nombre normal
            sb.AppendLine($"{localIndent}   Name            =   \"{ff.Source}\"");

            // Conversión aproximada
            double points = Math.Round(fs * 0.75, 2);
            sb.AppendLine($"{localIndent}   Size            =   {points}");

            sb.AppendLine($"{localIndent}   Charset         =   0");
            sb.AppendLine($"{localIndent}   Weight          =   {(fw == FontWeights.Bold ? 700 : 400)}");
            sb.AppendLine($"{localIndent}   Underline       =   0   'False");
            sb.AppendLine($"{localIndent}   Italic          =   {(fstyle == FontStyles.Italic ? -1 : 0)}   '{(fstyle == FontStyles.Italic ? "True" : "False")}");
            sb.AppendLine($"{localIndent}   Strikethrough   =   0   'False");
            sb.AppendLine($"{localIndent}EndProperty");
        }

        private static string GetVbType(FrameworkElement element)
        {
            // La fuente de verdad es el TAG. Si el tag dice "ResizeDirection", lo ignoramos.
            if (element.Tag is ResizeDirection) return "VB.Unknown";

            if (element.Tag is string tagType && !string.IsNullOrEmpty(tagType))
            {
                switch (tagType)
                {
                    case "CommandButton": return "VB.CommandButton";
                    case "Label": return "VB.Label";
                    case "TextBox": return "VB.TextBox";
                    case "CheckBox": return "VB.CheckBox";
                    case "OptionButton": return "VB.OptionButton";
                    case "Frame": return "VB.Frame";
                    case "PictureBox": return "VB.PictureBox";
                    case "ComboBox": return "VB.ComboBox";
                    case "ListBox": return "VB.ListBox";
                    case "Timer": return "VB.Timer";
                    case "Image": return "VB.Image";
                    case "Shape": return "VB.Shape";
                    case "Line": return "VB.Line";
                    // Controles Sheridan
                    case "SSPanel": return "Threed.SSPanel";
                    case "SSCommand": return "Threed.SSCommand";
                    case "SSCheck": return "Threed.SSCheck";
                    case "SSOption": return "Threed.SSOption";
                    case "SSFrame": return "Threed.SSFrame";
                }
            }

            // Fallback genérico (solo si no tiene Tag pero es un control nativo)
            if (element is Button) return "VB.CommandButton";
            if (element is Label) return "VB.Label";
            if (element is TextBox) return "VB.TextBox";

            // Si llegamos aquí, es basura visual (handles, adorners)
            return "VB.Unknown";
        }

        private static string ColorToVbHex(Color c)
        {
            // Formato VB6: &H00BBGGRR&
            return $"&H00{c.B:X2}{c.G:X2}{c.R:X2}&";
        }

        private static string SanitizeVbName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Form1";
            string clean = input.Replace(" ", "");
            char[] arr = clean.ToCharArray();
            clean = new string(Array.FindAll(arr, c => char.IsLetterOrDigit(c) || c == '_'));
            return string.IsNullOrEmpty(clean) ? "Form1" : clean;
        }

        private static Canvas GetChildCanvas(FrameworkElement container)
        {
            // Busca el canvas interno para recursividad
            if (container is GroupBox gb) return gb.Content as Canvas;
            if (container is Border b && b.Child is Canvas c) return c;
            // Para SSPanel que tiene un Grid dentro
            if (container is Border bp && bp.Child is Grid g)
            {
                foreach (var k in g.Children) if (k is Canvas c2) return c2;
            }
            return null;
        }
    }
}