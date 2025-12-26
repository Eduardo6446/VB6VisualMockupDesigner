using System;
using System.Collections.Generic;
using System.Text;

namespace VB6VisualMockupDesigner.Models
{
    // Estado para Undo/Redo
    public class ControlState
    {
        public string Name { get; set; }
        public string VbType { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Text { get; set; }
        public int ZIndex { get; set; }
        public int TabIndex { get; set; }
    }

    // Datos para Copiar/Pegar/Cortar
    public class ClipboardData
    {
        public string VbType { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public string Text { get; set; }
        public int ZIndex { get; set; }
        public int TabIndex { get; set; }
    }
}
