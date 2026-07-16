using System;
using System.IO;

namespace VB6VisualMockupDesigner.Helpers
{
    /// <summary>
    /// Provides logging functionality for the application, writing logs to daily files.
    /// </summary>
    public static class Logger
    {
        private static string _logPath;
        private static readonly object _lock = new object();

        static Logger()
        {
            // 1. Definir ruta: %AppData%\VB6VisualMockupDesigner\Logs
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "VB6VisualMockupDesigner", "Logs");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // 2. Nombre del archivo: log_YYYY-MM-DD.txt (Uno por día)
            string fileName = $"log_{DateTime.Now:yyyy-MM-dd}.txt";
            _logPath = Path.Combine(folder, fileName);
        }

        /// <summary>
        /// Logs a message to the daily log file with timestamp and type.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="type">The type of log message (default: "INFO").</param>
        public static void Log(string message, string type = "INFO")
        {
            try
            {
                string logLine = $"[{DateTime.Now:HH:mm:ss}] [{type}] {message}{Environment.NewLine}";

                // Bloqueo para evitar que dos hilos escriban a la vez
                lock (_lock)
                {
                    File.AppendAllText(_logPath, logLine);
                }
            }
            catch
            {
                // Si falla el log, no podemos hacer mucho, pero evitamos que tumbe la app
            }
        }

        /// <summary>
        /// Logs an exception with context information, including stack trace.
        /// </summary>
        /// <param name="context">The context where the error occurred.</param>
        /// <param name="ex">The exception to log.</param>
        public static void LogError(string context, Exception ex)
        {
            string msg = $"ERROR en {context}: {ex.Message}";
            if (ex.InnerException != null)
                msg += $" | Inner: {ex.InnerException.Message}";

            msg += $"\nStack Trace: {ex.StackTrace}";

            Log(msg, "ERROR");
        }
    }
}