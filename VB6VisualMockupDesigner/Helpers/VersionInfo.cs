using System;
using System.Collections.Generic;
using System.Text;

namespace VB6VisualMockupDesigner.Helpers
{
    /// <summary>
    /// Provides version information for the application.
    /// </summary>
    public static class VersionInfo
    {
        /// <summary>
        /// Major version number (SemVer).
        /// </summary>
        // Esquema SemVer: Major.Minor.Patch
        public const string Major = "0";
        
        /// <summary>
        /// Minor version number (SemVer).
        /// </summary>
        public const string Minor = "6";
        
        /// <summary>
        /// Patch version number (SemVer).
        /// </summary>
        public const string Patch = "0";

        /// <summary>
        /// Pre-release label (alpha, beta, rc).
        /// </summary>
        // Etiqueta pre-lanzamiento (alpha, beta, rc)
        public const string PreRelease = "alpha";

        /// <summary>
        /// Commercial name of the application.
        /// </summary>
        // Nombre comercial
        public const string AppName = "VB6 Visual Mockup Studio";

        /// <summary>
        /// Gets the full version string in SemVer format.
        /// </summary>
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

        /// <summary>
        /// Gets the formatted window title with application name and version.
        /// </summary>
        // Título formateado para la Ventana Principal
        public static string WindowTitle => $"{AppName} v{FullVersion}";
    }
}
