using System;
using System.Collections.Generic;
using System.Text;

namespace VB6VisualMockupDesigner.Models
{
    public class VbProject
    {
        public string Name { get; set; }
        public string StartupForm { get; set; }
        public List<VbFileReference> Files { get; set; } = new List<VbFileReference>();
        public string ProjectPath { get; set; }
    }

    public class VbFileReference
    {
        public string FileName { get; set; } // Ejemplo: Form1.frm
        public string FilePath { get; set; } // Ruta completa
        public string Type { get; set; }     // Form, Module, Class
        public List<ControlState> CachedState { get; set; } // Estado en memoria si se ha editado
    }
}
