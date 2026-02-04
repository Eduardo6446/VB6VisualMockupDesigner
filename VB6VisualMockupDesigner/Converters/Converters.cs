using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

// IMPORTANTE: Asegúrate de que este namespace coincida con el resto de tu proyecto.
// Si tu proyecto se llama diferente a "VB6VisualMockupDesigner", cámbialo aquí.
namespace VB6VisualMockupDesigner.Converters
{
    // =========================================================
    // CONVERTER 1: Para el ListBox de la Ventana (Maneja objetos FontFamily)
    // =========================================================
    
    /// <summary>
    /// Converts a FontFamily object to its display name.
    /// </summary>
    public class FontFamilyToNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ff = value as FontFamily;
            if (ff == null) return "";

            // 1. Intentar obtener nombre amigable
            string name = ff.FamilyNames.Values.FirstOrDefault();

            // 2. Si falla, usar el Source
            if (string.IsNullOrEmpty(name))
            {
                name = ff.Source;
            }

            // 3. Limpiar basura de rutas (file:///...)
            if (name.Contains("#"))
            {
                name = name.Split('#').Last();
            }

            return name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // =========================================================
    // CONVERTER 2: Para el Panel de Propiedades (Maneja Strings)
    // =========================================================
    
    /// <summary>
    /// Converts a font string to its display format.
    /// </summary>
    public class FontStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fullString = value as string;
            if (string.IsNullOrEmpty(fullString)) return "";

            // Formato: "file:///C:/...#Fuente; 12pt; Bold"
            var parts = fullString.Split(';');

            if (parts.Length > 0)
            {
                string family = parts[0];
                // Limpiamos la ruta si existe
                if (family.Contains("#"))
                {
                    parts[0] = family.Split('#').Last();
                }
            }

            return string.Join(";", parts);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}