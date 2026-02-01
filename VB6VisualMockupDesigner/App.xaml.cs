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
            string errorMsg = $"Error Crítico:\n{e.Exception.Message}\n\nLa aplicación se cerrará.";

            // 1. Mostrar el error al usuario
            MessageBox.Show(errorMsg, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);

            // 2. EVITAR QUE EL PROCESO QUEDE ZOMBIE
            // Environment.Exit(1) mata el proceso actual y devuelve código de error 1 al sistema.
            Environment.Exit(1);

            // (Opcional) Si Environment.Exit no funciona porque hay hilos rebeldes, usa esto:
            // System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

    }

}
