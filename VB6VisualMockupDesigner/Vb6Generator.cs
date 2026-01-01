using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VB6VisualMockupDesigner
{
    public static class Vb6Generator
    {
        // Factor de conversión (15 Twips = 1 Pixel aprox)
        private const double PxToTwip = 15.0;

        public static string GenerateFrmCode(Canvas designSurface, string formName = "Form1")
        {
            StringBuilder sb = new StringBuilder();

            // 1. Encabezado del Formulario
            sb.AppendLine($"VERSION 5.00");
            sb.AppendLine($"Begin VB.Form {formName} ");

            // Propiedades del Form (Simuladas)
            sb.AppendLine($"   Caption         =   \"{formName}\"");
            sb.AppendLine($"   ClientHeight    =   {(int)(designSurface.Height * PxToTwip)}");
            sb.AppendLine($"   ClientWidth     =   {(int)(designSurface.Width * PxToTwip)}");
            sb.AppendLine($"   ScaleHeight     =   {(int)(designSurface.Height * PxToTwip)}");
            sb.AppendLine($"   ScaleWidth      =   {(int)(designSurface.Width * PxToTwip)}");

            // 2. Iterar hijos (Recursivo)
            foreach (UIElement child in designSurface.Children)
            {
                if (child is FrameworkElement fe && !(child is Border)) // Ignorar adornos de selección
                {
                    AppendControlCode(sb, fe, 3); // Indentación inicial de 3 espacios
                }
            }

            sb.AppendLine("End"); // Fin del Form

            // Atributos extra simulados
            sb.AppendLine($"Attribute VB_Name = \"{formName}\"");
            sb.AppendLine("Attribute VB_GlobalNameSpace = False");
            sb.AppendLine("Attribute VB_Creatable = False");
            sb.AppendLine("Attribute VB_PredeclaredId = True");
            sb.AppendLine("Attribute VB_Exposed = False");

            return sb.ToString();
        }

        private static void AppendControlCode(StringBuilder sb, FrameworkElement ctrl, int indentLevel)
        {
            string indent = new string(' ', indentLevel);

            // Obtener Tipo y Nombre
            // El Tag lo pusimos en la Factory (ej: "CommandButton")
            string vbType = ctrl.Tag?.ToString();

            // Si el Tag es complejo (ej: "Array: 1"), habría que limpiarlo. 
            // Por simplicidad asumimos que Tag guarda el tipo limpio o lo inferimos.
            if (string.IsNullOrEmpty(vbType) || vbType.Contains(":"))
                vbType = ctrl.GetType().Name;

            // Mapeo rápido de nombres WPF a VB6 si es necesario
            if (vbType == "Button") vbType = "CommandButton";
            if (vbType == "TextBlock") vbType = "Label";

            // Generar nombre único (ej: Command1) si no tiene
            string name = ctrl.Name;
            if (string.IsNullOrEmpty(name)) name = $"{vbType}{Guid.NewGuid().ToString().Substring(0, 4)}";

            // Inicio del Bloque
            sb.AppendLine($"{indent}Begin VB.{vbType} {name} ");

            // Propiedades Comunes
            sb.AppendLine($"{indent}   Left            =   {(int)(Canvas.GetLeft(ctrl) * PxToTwip)}");
            sb.AppendLine($"{indent}   Top             =   {(int)(Canvas.GetTop(ctrl) * PxToTwip)}");
            sb.AppendLine($"{indent}   Width           =   {(int)(ctrl.Width * PxToTwip)}");
            sb.AppendLine($"{indent}   Height          =   {(int)(ctrl.Height * PxToTwip)}");

            // Propiedades Específicas
            if (ctrl is ContentControl cc && cc.Content is string caption)
            {
                sb.AppendLine($"{indent}   Caption         =   \"{CleanStr(caption)}\"");
            }
            else if (ctrl is TextBox tb)
            {
                sb.AppendLine($"{indent}   Text            =   \"{CleanStr(tb.Text)}\"");
            }
            else if (ctrl is TextBlock txt)
            {
                sb.AppendLine($"{indent}   Caption         =   \"{CleanStr(txt.Text)}\"");
            }

            // Manejo de Contenedores (Recursividad para Frame/PictureBox)
            // En nuestra implementación visual:
            // - Frame es un GroupBox -> Su contenido es un Canvas
            // - PictureBox es un Border -> Su hijo es un Canvas

            Canvas childCanvas = null;

            if (ctrl is GroupBox gb && gb.Content is Canvas gbc) childCanvas = gbc;
            else if (ctrl is Border b && b.Child is Canvas bc) childCanvas = bc;

            if (childCanvas != null)
            {
                foreach (UIElement grandChild in childCanvas.Children)
                {
                    if (grandChild is FrameworkElement gfe && !(grandChild is Border))
                    {
                        AppendControlCode(sb, gfe, indentLevel + 3);
                    }
                }
            }

            // Fin del Bloque
            sb.AppendLine($"{indent}End");
        }

        private static string CleanStr(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\"", "\"\""); // Escapar comillas
        }
    }
}