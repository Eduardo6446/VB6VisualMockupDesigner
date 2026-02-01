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

            Logger.Log("=== INICIO DE APLICACIÓN ===");

            // 2. Atrapar errores de la UI (Botones, Ventanas)
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            // 3. Atrapar errores de Hilos/Tasks (Background)
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            // 4. Atrapar errores graves del dominio (Crashes puros)
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        

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

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogError("UI Unhandled", e.Exception);

            // Opcional: Mostrar aviso al usuario
            MessageBox.Show($"Error inesperado:\n{e.Exception.Message}\nRevisa el log.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true; // Intentamos que no se cierre si es leve
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Logger.LogError("Background Task", e.Exception);
            e.SetObserved(); // Evita que el proceso muera
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logger.LogError("CRITICAL DOMAIN CRASH", ex);
                Logger.Log("La aplicación se cerrará forzosamente.", "FATAL");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Log("=== FIN DE APLICACIÓN ===");
            base.OnExit(e);
        }




    }

}
