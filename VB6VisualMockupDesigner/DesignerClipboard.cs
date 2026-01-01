using System.Collections.Generic;
using System.Windows.Media;

namespace VB6VisualMockupDesigner
{
    // Datos para el Portapapeles (Copiar/Pegar)
    public static class DesignerClipboard
    {
        public static string ControlType { get; set; }
        public static double Width { get; set; }
        public static double Height { get; set; }
        public static string ContentText { get; set; } // Caption o Text
        public static bool IsEmpty => string.IsNullOrEmpty(ControlType);
    }

    // Datos para el Historial (Undo/Redo)
    public class CanvasState
    {
        public List<ControlSnapshot> Controls { get; set; } = new List<ControlSnapshot>();
    }

    public class ControlSnapshot
    {
        public string Type { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Text { get; set; }
        public string Tag { get; set; } // Para guardar índices de arrays o nombres
    }
}