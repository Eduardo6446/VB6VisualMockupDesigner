using System;
using System.Collections.Generic;
using System.Text;

namespace VB6VisualMockupDesigner.Models
{
    public class RecentFile
    {
        public string Name { get; set; }      // Ej: frmLogin.frm
        public string FullPath { get; set; }  // Ej: C:\Proyectos\frmLogin.frm
        public DateTime LastAccess { get; set; } // Para ordenar o mostrar la fecha
    }
}
