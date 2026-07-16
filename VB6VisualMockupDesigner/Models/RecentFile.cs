using System;
using System.Collections.Generic;
using System.Text;

namespace VB6VisualMockupDesigner.Models
{
    /// <summary>
    /// Represents a recently opened file.
    /// </summary>
    public class RecentFile
    {
        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string Name { get; set; }      // Ej: frmLogin.frm
        
        /// <summary>
        /// Gets or sets the full path to the file.
        /// </summary>
        public string FullPath { get; set; }  // Ej: C:\Proyectos\frmLogin.frm
        
        /// <summary>
        /// Gets or sets the last access date and time.
        /// </summary>
        public DateTime LastAccess { get; set; } // Para ordenar o mostrar la fecha
    }
}
