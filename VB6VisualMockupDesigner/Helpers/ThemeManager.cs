using System.Windows;
using System.Windows.Media;

namespace VB6VisualMockupDesigner
{
    public static class ThemeManager
    {
        // Definimos los tipos de temas disponibles
        public enum ThemeType
        {
            Dark,
            Light,
            VB6,
            Custom
        }

        public static ThemeType CurrentTheme { get; private set; } = ThemeType.Dark;

        public static void ApplyTheme(ThemeType theme)
        {
            CurrentTheme = theme;
            var dict = new ResourceDictionary();

            switch (theme)
            {
                case ThemeType.Dark:
                    // Colores idénticos a los definidos por defecto en App.xaml
                    SetColor(dict, "AppBackground", "#1E1E1E");
                    SetColor(dict, "PanelBackground", "#252526");
                    SetColor(dict, "SideBarBackground", "#252526");
                    SetColor(dict, "HeaderBackground", "#2D2D30");
                    SetColor(dict, "BorderBrush", "#3E3E42");
                    SetColor(dict, "HoverBrush", "#3E3E42");
                    SetColor(dict, "PressedBrush", "#007ACC");
                    SetColor(dict, "PrimaryText", "#F1F1F1");
                    SetColor(dict, "SecondaryText", "#969696");
                    SetColor(dict, "IconColor", "#C5C5C5");
                    SetColor(dict, "BrandColor", "#005A9E");
                    SetColor(dict, "AccentColor", "#007ACC");
                    break;

                case ThemeType.Light:
                    // Tema claro inspirado en Visual Studio Light
                    SetColor(dict, "AppBackground", "#F3F3F3");
                    SetColor(dict, "PanelBackground", "#FFFFFF");
                    SetColor(dict, "SideBarBackground", "#F3F3F3");
                    SetColor(dict, "HeaderBackground", "#EFEFEF");
                    SetColor(dict, "BorderBrush", "#CCCCCC");
                    SetColor(dict, "HoverBrush", "#E6E6E6");
                    SetColor(dict, "PressedBrush", "#0078D7");
                    SetColor(dict, "PrimaryText", "#333333");
                    SetColor(dict, "SecondaryText", "#666666");
                    SetColor(dict, "IconColor", "#444444");
                    SetColor(dict, "BrandColor", "#0078D7");
                    SetColor(dict, "AccentColor", "#0078D7");
                    break;

                case ThemeType.VB6:
                    // Estilo Clásico Windows 98/2000
                    SetColor(dict, "AppBackground", "#004080"); // Fondo MDI clásico
                    SetColor(dict, "PanelBackground", "#D4D0C8"); // ButtonFace
                    SetColor(dict, "SideBarBackground", "#D4D0C8");
                    SetColor(dict, "HeaderBackground", "#D4D0C8");
                    SetColor(dict, "BorderBrush", "#808080"); // ButtonShadow
                    SetColor(dict, "HoverBrush", "#0A246A"); // Highlight
                    SetColor(dict, "PressedBrush", "#000080");
                    SetColor(dict, "PrimaryText", "#000000");
                    SetColor(dict, "SecondaryText", "#404040");
                    SetColor(dict, "IconColor", "#000000");
                    SetColor(dict, "BrandColor", "#000080");
                    SetColor(dict, "AccentColor", "#000080");
                    break;

                case ThemeType.Custom:
                    // Ejemplo: Alto Contraste / Matrix
                    SetColor(dict, "AppBackground", "#000000");
                    SetColor(dict, "PanelBackground", "#111111");
                    SetColor(dict, "SideBarBackground", "#000000");
                    SetColor(dict, "HeaderBackground", "#222222");
                    SetColor(dict, "BorderBrush", "#00FF00");
                    SetColor(dict, "HoverBrush", "#003300");
                    SetColor(dict, "PressedBrush", "#00FF00");
                    SetColor(dict, "PrimaryText", "#00FF00");
                    SetColor(dict, "SecondaryText", "#008800");
                    SetColor(dict, "IconColor", "#00FF00");
                    SetColor(dict, "BrandColor", "#00FF00");
                    SetColor(dict, "AccentColor", "#00FF00");
                    break;
            }

            // Reemplaza los recursos existentes. Al ser DynamicResource, la UI se actualiza sola.
            foreach (var key in dict.Keys)
            {
                Application.Current.Resources[key] = dict[key];
            }
        }

        private static void SetColor(ResourceDictionary dict, string key, string hexColor)
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            dict.Add(key, new SolidColorBrush(color));
        }
    }
}