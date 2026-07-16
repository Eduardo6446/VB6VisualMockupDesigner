using System;
using System.Collections.Generic;
using System.Text;

namespace VB6VisualMockupDesigner.Models
{
    /// <summary>
    /// Represents a VB6 project structure.
    /// </summary>
    public class VbProject
    {
        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the startup form name.
        /// </summary>
        public string StartupForm { get; set; }
        
        /// <summary>
        /// Gets or sets the list of file references in the project.
        /// </summary>
        public List<VbFileReference> Files { get; set; } = new List<VbFileReference>();
        
        /// <summary>
        /// Gets or sets the full path to the project file.
        /// </summary>
        public string ProjectPath { get; set; }
    }

    /// <summary>
    /// Represents a reference to a VB6 file within a project.
    /// </summary>
    public class VbFileReference
    {
        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; } // Ejemplo: Form1.frm
        
        /// <summary>
        /// Gets or sets the full file path.
        /// </summary>
        public string FilePath { get; set; } // Ruta completa
        
        /// <summary>
        /// Gets or sets the file type (Form, Module, Class).
        /// </summary>
        public string Type { get; set; }     // Form, Module, Class
        
        /// <summary>
        /// Gets or sets the cached state if the file has been edited in memory.
        /// </summary>
        public List<ControlState> CachedState { get; set; } // Estado en memoria si se ha editado
    }
}
