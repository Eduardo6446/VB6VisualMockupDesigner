using System.Collections.Generic;
using System.Windows.Media;
using VB6VisualMockupDesigner.Views; // Requiere .NET Core 3.1 o superior (o NuGet en .NET Framework)
using VB6VisualMockupDesigner.Helpers;
using VB6VisualMockupDesigner.Controls;
using VB6VisualMockupDesigner.Services;

namespace VB6VisualMockupDesigner.Models
{
    // Datos para el Portapapeles (Copiar/Pegar)
    public static class DesignerClipboard
    {
        // Ahora guardamos una LISTA de objetos, no propiedades sueltas
        public static List<ClipboardItem> Items { get; set; } = new List<ClipboardItem>();

        public static bool IsEmpty => Items == null || Items.Count == 0;

        public static void Clear() => Items.Clear();
    }

    public class ClipboardItem
    {
        public string ControlType { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string ContentText { get; set; }

        // IMPORTANTE: Guardamos la posición relativa al grupo.
        // Ejemplo: Si copio un grupo, no quiero posiciones absolutas (X=500), 
        // quiero saber qué tan lejos está este botón del primero que copié.
        public double RelativeLeft { get; set; }
        public double RelativeTop { get; set; }
    }

    // ==========================================
    // 2. HISTORIAL UNDO/REDO (Snapshots)
    // ==========================================
    public class CanvasState
    {
        // Aquí guardamos la foto completa del lienzo
        public List<ControlSnapshot> Controls { get; set; } = new List<ControlSnapshot>();
        public Guid VersionId { get; set; }
    }

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
    }
}