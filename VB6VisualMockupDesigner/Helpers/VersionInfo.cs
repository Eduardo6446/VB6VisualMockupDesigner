using System;
using System.Collections.Generic;
using System.Text;

namespace VB6VisualMockupDesigner.Helpers
{
    public static class VersionInfo
    {
        // Esquema SemVer: Major.Minor.Patch
        public const string Major = "0";
        public const string Minor = "6";
        public const string Patch = "0";

        // Etiqueta pre-lanzamiento (alpha, beta, rc)
        public const string PreRelease = "alpha";

        // Nombre comercial
        public const string AppName = "VB6 Visual Mockup Studio";

        // Propiedad para obtener el string completo
        public static string FullVersion
        {
            get
            {
                string ver = $"{Major}.{Minor}.{Patch}";
                if (!string.IsNullOrEmpty(PreRelease))
                {
                    ver += $"-{PreRelease}";
                }
                return ver;
            }
        }

        // Título formateado para la Ventana Principal
        public static string WindowTitle => $"{AppName} v{FullVersion}";
    }
}
