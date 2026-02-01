using System.Configuration;
using System.Data;
using System.Windows;
using VB6VisualMockupDesigner.Helpers;

namespace VB6VisualMockupDesigner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Leer la configuración guardada
            string savedTheme = VB6VisualMockupDesigner.Properties.Settings.Default.AppTheme;

            // 2. Decidir qué tema aplicar
            if (string.IsNullOrEmpty(savedTheme))
            {
                savedTheme = "Dark"; // Valor por defecto si es la primera vez
            }

            // 3. Convertir el string guardado (ej: "Light") al Enum y aplicar
            switch (savedTheme)
            {
                case "Light":
                    ThemeManager.ApplyTheme(ThemeManager.ThemeType.Light);
                    break;
                case "Personalized":
                    ThemeManager.ApplyTheme(ThemeManager.ThemeType.Personalized);
                    break;
                case "HighContrast":
                    ThemeManager.ApplyTheme(ThemeManager.ThemeType.HighContrast);
                    break;
                default: // Dark
                    ThemeManager.ApplyTheme(ThemeManager.ThemeType.Dark);
                    break;
            }
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // ESTO EVITA QUE LA APP SE CIERRE Y MUESTRA EL ERROR
            string errorMsg = $"Ocurrió un error inesperado:\n\n{e.Exception.Message}";

            if (e.Exception.InnerException != null)
            {
                errorMsg += $"\n\nDetalle interno: {e.Exception.InnerException.Message}";
            }

            MessageBox.Show(errorMsg, "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true; // Evita el cierre inmediato si es posible recuperarse
        }

    }

}
