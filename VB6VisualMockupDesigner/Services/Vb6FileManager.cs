using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Para VisualTreeHelper si fuera necesario
using VB6VisualMockupDesigner.Helpers;

namespace VB6VisualMockupDesigner.Services
{
    public class Vb6FileManager
    {
        private ControlFactory _factory;

        public Vb6FileManager(ControlFactory factory)
        {
            _factory = factory;
        }

        public void ParseVb6Frm(string filePath, Canvas canvas, Action<FrameworkElement> registerControlCallback)
        {
            canvas.Children.Clear();
            // Nota: El SelectionBox se debe volver a agregar o manejar en la ventana, 
            // aquí limpiamos todo. Es mejor que la ventana gestione el SelectionBox.

            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("Begin VB."))
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length < 3) continue;
                    string type = parts[1].Replace("VB.", "");
                    string name = parts[2];

                    CreateControlFromImport(type, name, lines, i, registerControlCallback);
                }
            }
        }

        private void CreateControlFromImport(string vbType, string name, string[] allLines, int startIndex, Action<FrameworkElement> registerControlCallback)
        {
            FrameworkElement ctrl = _factory.CreateElementInstance(vbType);
            if (ctrl == null) return;

            ctrl.Name = name;
            double left = 0, top = 0, width = 100, height = 30;
            string caption = name;

            for (int j = startIndex + 1; j < allLines.Length; j++)
            {
                string l = allLines[j].Trim();
                if (l == "End") break;
                if (l.StartsWith("Begin ")) break;
                if (l.Contains("="))
                {
                    string prop = l.Split('=')[0].Trim();
                    string val = l.Split('=')[1].Trim().Replace("\"", "");
                    switch (prop)
                    {
                        case "Caption": case "Text": caption = val; break;
                        case "Left": left = double.Parse(val) / VbHelpers.PixelsToTwips; break;
                        case "Top": top = double.Parse(val) / VbHelpers.PixelsToTwips; break;
                        case "Width": width = double.Parse(val) / VbHelpers.PixelsToTwips; break;
                        case "Height": height = double.Parse(val) / VbHelpers.PixelsToTwips; break;
                    }
                }
            }
            ctrl.Width = width; ctrl.Height = height;

            VbHelpers.SetControlText(ctrl, caption);

            // Establecer posición antes de registrar
            Canvas.SetLeft(ctrl, Math.Round(left / 8) * 8);
            Canvas.SetTop(ctrl, Math.Round(top / 8) * 8);

            registerControlCallback(ctrl);
        }

        public string GenerateVb6Code(Canvas canvas, UIElement selectionBox)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("VERSION 5.00");
            sb.AppendLine("Begin VB.Form Form1");
            sb.AppendLine("   Caption         =   \"Mockup\"");
            sb.AppendLine($"   ClientHeight    =   {canvas.ActualHeight * VbHelpers.PixelsToTwips}");
            sb.AppendLine($"   ClientWidth     =   {canvas.ActualWidth * VbHelpers.PixelsToTwips}");

            foreach (UIElement child in canvas.Children)
            {
                if (child is FrameworkElement fe && child != selectionBox)
                {
                    string vbType = VbHelpers.MapWpfToVbType(fe);
                    if (string.IsNullOrEmpty(vbType)) continue;

                    if (vbType == "Menu")
                    {
                        sb.AppendLine($"   Begin VB.Menu {fe.Name}");
                        sb.AppendLine($"      Caption = \"{VbHelpers.GetControlText(fe)}\"");
                        sb.AppendLine("   End");
                        continue;
                    }

                    sb.AppendLine($"   Begin VB.{vbType} {fe.Name}");
                    sb.AppendLine($"      Left            =   {Math.Round(Canvas.GetLeft(fe) * VbHelpers.PixelsToTwips)}");
                    sb.AppendLine($"      Top             =   {Math.Round(Canvas.GetTop(fe) * VbHelpers.PixelsToTwips)}");
                    sb.AppendLine($"      Width           =   {Math.Round(fe.Width * VbHelpers.PixelsToTwips)}");
                    sb.AppendLine($"      Height          =   {Math.Round(fe.Height * VbHelpers.PixelsToTwips)}");

                    string txt = VbHelpers.GetControlText(fe);
                    if (fe is TextBox || fe is ComboBox) sb.AppendLine($"      Text            =   \"{txt}\"");
                    else sb.AppendLine($"      Caption         =   \"{txt}\"");

                    sb.AppendLine("   End");
                }
            }
            sb.AppendLine("End");
            return sb.ToString();
        }
    }
}