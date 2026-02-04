using System.Collections.Generic;
using System.Windows.Media;
using VB6VisualMockupDesigner.Views; // Requiere .NET Core 3.1 o superior (o NuGet en .NET Framework)
using VB6VisualMockupDesigner.Helpers;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Services;

namespace VB6VisualMockupDesigner.Models
{
    /// <summary>
    /// Manages the designer clipboard for copy/paste operations.
    /// </summary>
    // Datos para el Portapapeles (Copiar/Pegar)
    public static class DesignerClipboard
    {
        /// <summary>
        /// Gets or sets the list of clipboard items.
        /// </summary>
        // Ahora guardamos una LISTA de objetos, no propiedades sueltas
        public static List<ClipboardItem> Items { get; set; } = new List<ClipboardItem>();

        /// <summary>
        /// Gets a value indicating whether the clipboard is empty.
        /// </summary>
        public static bool IsEmpty => Items == null || Items.Count == 0;

        /// <summary>
        /// Clears the clipboard.
        /// </summary>
        public static void Clear() => Items.Clear();
    }

    /// <summary>
    /// Represents a single item in the clipboard.
    /// </summary>
    public class ClipboardItem
    {
        /// <summary>
        /// Gets or sets the control type.
        /// </summary>
        public string ControlType { get; set; }
        
        /// <summary>
        /// Gets or sets the control width.
        /// </summary>
        public double Width { get; set; }
        
        /// <summary>
        /// Gets or sets the control height.
        /// </summary>
        public double Height { get; set; }
        
        /// <summary>
        /// Gets or sets the control content text.
        /// </summary>
        public string ContentText { get; set; }

        /// <summary>
        /// Gets or sets the relative left position within a group.
        /// </summary>
        // IMPORTANTE: Guardamos la posición relativa al grupo.
        // Ejemplo: Si copio un grupo, no quiero posiciones absolutas (X=500), 
        // quiero saber qué tan lejos está este botón del primero que copié.
        public double RelativeLeft { get; set; }
        
        /// <summary>
        /// Gets or sets the relative top position within a group.
        /// </summary>
        public double RelativeTop { get; set; }
    }

    /// <summary>
    /// Represents a snapshot of the canvas state for undo/redo operations.
    /// </summary>
    // ==========================================
    // 2. HISTORIAL UNDO/REDO (Snapshots)
    // ==========================================
    public class CanvasState
    {
        /// <summary>
        /// Gets or sets the list of control snapshots.
        /// </summary>
        // Aquí guardamos la foto completa del lienzo
        public List<ControlSnapshot> Controls { get; set; } = new List<ControlSnapshot>();
        
        /// <summary>
        /// Gets or sets the unique version identifier for this state.
        /// </summary>
        public Guid VersionId { get; set; }
    }

    /// <summary>
    /// Represents a snapshot of a single control's state.
    /// </summary>
    public class ControlSnapshot
    {
        public string Type { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Text { get; set; }
        public string Tag { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
        //public string ParentName { get; set; } // ¿Quién es mi padre?
    }
}