using System.Windows;
using System.Windows.Media;

namespace VB6VisualMockupDesigner.Helpers
{
    public static class ThemeManager
    {
        public enum ThemeType
        {
            Dark,
            Light,
            Personalized,
            HighContrast,
            SoftRose,
            CyberYellow,
            Blueprint
        }

        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Dark;

        public static void ApplyTheme(ThemeType theme)
        {
            CurrentTheme = theme;
            var dict = new ResourceDictionary();

            switch (theme)
            {
                case ThemeType.Dark:
                    // Base
                    SetColor(dict, "AppBackground", "#1E1E1E");
                    SetColor(dict, "PanelBackground", "#252526");
                    SetColor(dict, "SideBarBackground", "#252526");
                    SetColor(dict, "HeaderBackground", "#2D2D30");
                    SetColor(dict, "BorderBrush", "#3E3E42");

                    // Texto e Iconos
                    SetColor(dict, "PrimaryText", "#F1F1F1");
                    SetColor(dict, "SecondaryText", "#969696");
                    SetColor(dict, "IconColor", "#C5C5C5");
                    SetColor(dict, "SuccessText", "#4CAF50"); // Verde memoria

                    // Interacción
                    SetColor(dict, "HoverBrush", "#3E3E42");
                    SetColor(dict, "PressedBrush", "#007ACC");
                    SetColor(dict, "BrandColor", "#005A9E");
                    SetColor(dict, "AccentColor", "#007ACC");

                    // Específicos para Listas/Árboles (Project Explorer)
                    SetColor(dict, "TreeHoverBrush", "#2A2D2E");
                    SetColor(dict, "TreeSelectedBrush", "#37373D");
                    SetColor(dict, "TreeArrowColor", "#888888");

                    // Canvas
                    SetColor(dict, "CanvasGridColor", "#707070");
                    SetColor(dict, "SelectionFill", "#336495ED"); // Azul semitransparente
                    break;

                case ThemeType.Light:
                    SetColor(dict, "AppBackground", "#F3F3F3");
                    SetColor(dict, "PanelBackground", "#FFFFFF");
                    SetColor(dict, "SideBarBackground", "#F3F3F3");
                    SetColor(dict, "HeaderBackground", "#EFEFEF");
                    SetColor(dict, "BorderBrush", "#CCCCCC");

                    SetColor(dict, "PrimaryText", "#333333");
                    SetColor(dict, "SecondaryText", "#666666");
                    SetColor(dict, "IconColor", "#444444");
                    SetColor(dict, "SuccessText", "#2E7D32");

                    SetColor(dict, "HoverBrush", "#E6E6E6");
                    SetColor(dict, "PressedBrush", "#CCE8FF");
                    SetColor(dict, "BrandColor", "#0078D7");
                    SetColor(dict, "AccentColor", "#0078D7");

                    SetColor(dict, "TreeHoverBrush", "#E8E8E8");
                    SetColor(dict, "TreeSelectedBrush", "#CCE8FF");
                    SetColor(dict, "TreeArrowColor", "#666666");

                    SetColor(dict, "CanvasGridColor", "#A0A0A0");
                    SetColor(dict, "SelectionFill", "#330078D7");
                    break;

                case ThemeType.Personalized:
                    // 1. Cargar todas las settings (con fallbacks por si acaso)
                    string appBg = GetSetting("Cust_AppBg", "#004080");
                    string panelBg = GetSetting("Cust_PanelBg", "#D4D0C8");
                    string text = GetSetting("Cust_Text", "#000000");
                    string accent = GetSetting("Cust_Accent", "#000080");

                    // Nuevas variables extendidas
                    string secText = GetSetting("Cust_SecText", "#606060");
                    string border = GetSetting("Cust_Border", "#808080");
                    string grid = GetSetting("Cust_Grid", "#808080");
                    string selection = GetSetting("Cust_Selection", "#40000080");

                    // 2. Asignar Colores Base
                    SetColor(dict, "AppBackground", appBg);
                    SetColor(dict, "PanelBackground", panelBg);
                    SetColor(dict, "SideBarBackground", panelBg); // Reutilizamos panel para consistencia
                    SetColor(dict, "HeaderBackground", panelBg);

                    // 3. Detalles Finos (NUEVO)
                    SetColor(dict, "BorderBrush", border);
                    SetColor(dict, "HoverBrush", "#20FFFFFF"); // Un blanco sutil para hover funciona en casi todo
                    SetColor(dict, "PressedBrush", accent);

                    // 4. Textos e Iconos (NUEVO)
                    SetColor(dict, "PrimaryText", text);
                    SetColor(dict, "SecondaryText", secText);
                    SetColor(dict, "IconColor", text);
                    SetColor(dict, "SuccessText", "#008000"); // Verde fijo está bien, o podrías parametrizarlo

                    // 5. Marca y Acento
                    SetColor(dict, "BrandColor", accent);
                    SetColor(dict, "AccentColor", accent);

                    // 6. Árbol de Proyecto (Derivado de los anteriores para armonía)
                    SetColor(dict, "TreeHoverBrush", "#20000000");
                    SetColor(dict, "TreeSelectedBrush", "#40000000");
                    SetColor(dict, "TreeArrowColor", text);

                    // 7. Canvas (NUEVO)
                    SetColor(dict, "CanvasGridColor", grid);
                    SetColor(dict, "SelectionFill", selection);
                    break;

                case ThemeType.HighContrast:
                    SetColor(dict, "AppBackground", "#000000");
                    SetColor(dict, "PanelBackground", "#000000");
                    SetColor(dict, "SideBarBackground", "#000000");
                    SetColor(dict, "HeaderBackground", "#000000");
                    SetColor(dict, "BorderBrush", "#00FF00");

                    SetColor(dict, "PrimaryText", "#00FF00");
                    SetColor(dict, "SecondaryText", "#008800");
                    SetColor(dict, "IconColor", "#00FF00");
                    SetColor(dict, "SuccessText", "#00FF00");

                    SetColor(dict, "HoverBrush", "#003300");
                    SetColor(dict, "PressedBrush", "#00FF00");
                    SetColor(dict, "BrandColor", "#00FF00");
                    SetColor(dict, "AccentColor", "#00FF00");

                    SetColor(dict, "TreeHoverBrush", "#003300");
                    SetColor(dict, "TreeSelectedBrush", "#004400");
                    SetColor(dict, "TreeArrowColor", "#00FF00");

                    SetColor(dict, "CanvasGridColor", "#005500");
                    SetColor(dict, "SelectionFill", "#4000FF00");
                    break;
                case ThemeType.SoftRose:
                    SetColor(dict, "AppBackground", "#FFF0F5"); // Lavender Blush
                    SetColor(dict, "PanelBackground", "#FFFFFF");
                    SetColor(dict, "SideBarBackground", "#FFF0F5");
                    SetColor(dict, "HeaderBackground", "#FFE4E1"); // Misty Rose
                    SetColor(dict, "BorderBrush", "#FFB6C1"); // Light Pink

                    SetColor(dict, "PrimaryText", "#4A4A4A"); // Gris oscuro suave
                    SetColor(dict, "SecondaryText", "#885566");
                    SetColor(dict, "IconColor", "#C71585"); // Medium Violet Red
                    SetColor(dict, "SuccessText", "#2E7D32");

                    SetColor(dict, "HoverBrush", "#FFF5EE"); // Seashell
                    SetColor(dict, "PressedBrush", "#FF69B4");
                    SetColor(dict, "BrandColor", "#DB7093"); // Pale Violet Red
                    SetColor(dict, "AccentColor", "#FF1493"); // Deep Pink

                    SetColor(dict, "TreeHoverBrush", "#FFF5F8");
                    SetColor(dict, "TreeSelectedBrush", "#FFEBF0");
                    SetColor(dict, "TreeArrowColor", "#DB7093");

                    SetColor(dict, "CanvasGridColor", "#FFC0CB");
                    SetColor(dict, "SelectionFill", "#40FF1493");
                    break;

                // 2. TEMA AMARILLO (Cyber Yellow / Construcción)
                case ThemeType.CyberYellow:
                    SetColor(dict, "AppBackground", "#F5F5F5");
                    SetColor(dict, "PanelBackground", "#FFFFFF");
                    SetColor(dict, "SideBarBackground", "#FAFAFA");

                    // ANTES: #1A1A1A (Negro) -> AHORA: #EEEEEE (Gris muy claro)
                    // Esto elimina la franja negra fea detrás de las pestañas
                    SetColor(dict, "HeaderBackground", "#EEEEEE");

                    SetColor(dict, "BorderBrush", "#E0E0E0");

                    SetColor(dict, "PrimaryText", "#212121");
                    SetColor(dict, "SecondaryText", "#757575");
                    SetColor(dict, "IconColor", "#FBC02D");
                    SetColor(dict, "SuccessText", "#388E3C");

                    SetColor(dict, "HoverBrush", "#FFFDE7"); // Amarillo muy pálido al pasar el mouse
                    SetColor(dict, "PressedBrush", "#FFEB3B");
                    SetColor(dict, "BrandColor", "#FBC02D"); // Amarillo Ocre
                    SetColor(dict, "AccentColor", "#FFD600"); // Amarillo Eléctrico

                    SetColor(dict, "TreeHoverBrush", "#FFF8E1");
                    SetColor(dict, "TreeSelectedBrush", "#FFF9C4");
                    SetColor(dict, "TreeArrowColor", "#FBC02D");

                    SetColor(dict, "CanvasGridColor", "#CCCCCC");
                    SetColor(dict, "SelectionFill", "#40FFD600");
                    break;

                // 3. TEMA BLUEPRINT (Arquitecto) - Mi favorito para Mockups
                case ThemeType.Blueprint:
                    SetColor(dict, "AppBackground", "#002B49"); // Azul plano oscuro
                    SetColor(dict, "PanelBackground", "#003355");
                    SetColor(dict, "SideBarBackground", "#00223A");
                    SetColor(dict, "HeaderBackground", "#001D33");
                    SetColor(dict, "BorderBrush", "#005588");

                    SetColor(dict, "PrimaryText", "#FFFFFF");
                    SetColor(dict, "SecondaryText", "#88CCFF");
                    SetColor(dict, "IconColor", "#FFFFFF");
                    SetColor(dict, "SuccessText", "#44FF88");

                    SetColor(dict, "HoverBrush", "#20FFFFFF");
                    SetColor(dict, "PressedBrush", "#0077BB");
                    SetColor(dict, "BrandColor", "#0088CC");
                    SetColor(dict, "AccentColor", "#FFFFFF"); // Blanco puro para resaltar

                    SetColor(dict, "TreeHoverBrush", "#30FFFFFF");
                    SetColor(dict, "TreeSelectedBrush", "#50FFFFFF");
                    SetColor(dict, "TreeArrowColor", "#FFFFFF");

                    SetColor(dict, "CanvasGridColor", "#40FFFFFF"); // Grid blanca tenue
                    SetColor(dict, "SelectionFill", "#30FFFFFF");
                    break;
            }

            foreach (var key in dict.Keys)
            {
                Application.Current.Resources[key] = dict[key];
            }
        }

        // Helper para leer settings de forma segura
        private static string GetSetting(string key, string defaultHex)
        {
            try
            {
                string val = (string)Properties.Settings.Default[key];
                return string.IsNullOrEmpty(val) ? defaultHex : val;
            }
            catch { return defaultHex; }
        }

        private static void SetColor(ResourceDictionary dict, string key, string hexColor)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                dict.Add(key, new SolidColorBrush(color));
            }
            catch
            {
                dict.Add(key, new SolidColorBrush(Colors.Red));
            }
        }

        public static void UpdateSingleColor(string resourceKey, string hexColor)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hexColor);
                Application.Current.Resources[resourceKey] = new SolidColorBrush(color);
            }
            catch { }
        }
    }
}