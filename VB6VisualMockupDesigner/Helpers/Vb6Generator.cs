using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using VB6VisualMockupDesigner.Helpers; // Ajusta tus namespaces

public static class Vb6Generator
{
    private const double PxToTwip = 15.0;

    public static string GenerateFrmCode(Canvas designSurface, string formName = "Form1")
    {
        StringBuilder sb = new StringBuilder();

        // FIX 1: Usar ActualWidth/Height para el Formulario también, por si el Canvas está en Auto
        double frmWidth = double.IsNaN(designSurface.Width) ? designSurface.ActualWidth : designSurface.Width;
        double frmHeight = double.IsNaN(designSurface.Height) ? designSurface.ActualHeight : designSurface.Height;

        sb.AppendLine($"VERSION 5.00");
        sb.AppendLine($"Begin VB.Form {formName} ");
        sb.AppendLine($"   Caption         =   \"{formName}\"");
        sb.AppendLine($"   ClientHeight    =   {(int)(frmHeight * PxToTwip)}");
        sb.AppendLine($"   ClientWidth     =   {(int)(frmWidth * PxToTwip)}");
        sb.AppendLine($"   ScaleHeight     =   {(int)(frmHeight * PxToTwip)}");
        sb.AppendLine($"   ScaleWidth      =   {(int)(frmWidth * PxToTwip)}");

        foreach (UIElement child in designSurface.Children)
        {
            if (child is FrameworkElement fe && !(child is Border))
            {
                AppendControlCode(sb, fe, 3);
            }
        }

        sb.AppendLine("End");
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
        string vbType = ctrl.Tag?.ToString();

        if (string.IsNullOrEmpty(vbType) || vbType.Contains(":"))
            vbType = ctrl.GetType().Name;

        if (vbType == "Button") vbType = "CommandButton";
        if (vbType == "TextBlock") vbType = "Label";

        string name = ctrl.Name;
        if (string.IsNullOrEmpty(name)) name = $"{vbType}{Guid.NewGuid().ToString().Substring(0, 4)}";

        sb.AppendLine($"{indent}Begin VB.{vbType} {name} ");

        // FIX 2: Lógica segura para Posición (Left/Top)
        // Si nunca se movió el control, GetLeft retorna NaN. Asumimos 0.
        double left = Canvas.GetLeft(ctrl);
        if (double.IsNaN(left)) left = 0;

        double top = Canvas.GetTop(ctrl);
        if (double.IsNaN(top)) top = 0;

        // FIX 3: Lógica segura para Tamaño (Width/Height)
        // Si Width es NaN (Auto), usamos ActualWidth (lo que se ve en pantalla)
        double width = ctrl.Width;
        if (double.IsNaN(width)) width = ctrl.ActualWidth;

        double height = ctrl.Height;
        if (double.IsNaN(height)) height = ctrl.ActualHeight;

        sb.AppendLine($"{indent}   Left            =   {(int)(left * PxToTwip)}");
        sb.AppendLine($"{indent}   Top             =   {(int)(top * PxToTwip)}");
        sb.AppendLine($"{indent}   Width           =   {(int)(width * PxToTwip)}");
        sb.AppendLine($"{indent}   Height          =   {(int)(height * PxToTwip)}");

        // FIX 4: Recuperar otras propiedades que mencionaste perder
        // Debes mapear propiedades de WPF a VB6 manualmente
        if (ctrl.IsEnabled == false) sb.AppendLine($"{indent}   Enabled         =   0   'False");
        if (ctrl.Visibility != Visibility.Visible) sb.AppendLine($"{indent}   Visible         =   0   'False");

        // Propiedades Específicas de Texto
        if (ctrl is ContentControl cc && cc.Content is string caption)
        {
            sb.AppendLine($"{indent}   Caption         =   \"{CleanStr(caption)}\"");
        }
        else if (ctrl is TextBox tb)
        {
            sb.AppendLine($"{indent}   Text            =   \"{CleanStr(tb.Text)}\"");
            // Ejemplo: Mapear Multiline
            if (tb.AcceptsReturn) sb.AppendLine($"{indent}   MultiLine       =   -1  'True");
        }
        else if (ctrl is TextBlock txt)
        {
            sb.AppendLine($"{indent}   Caption         =   \"{CleanStr(txt.Text)}\"");
            // Ejemplo: Alignment (0=Left, 1=Right, 2=Center)
            if (txt.TextAlignment == TextAlignment.Center) sb.AppendLine($"{indent}   Alignment       =   2");
        }

        // Lógica de contenedores recursivos...
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

        sb.AppendLine($"{indent}End");
    }

    private static string CleanStr(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\"", "\"\"");
    }
}