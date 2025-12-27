using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using VB6VisualMockupDesigner.Models; // Requiere .NET Core 3.1 o superior (o NuGet en .NET Framework)


namespace VB6VisualMockupDesigner
{
    public static class RecentFilesManager
    {
        private const string AppName = "VB6VisualMockupDesigner";
        private const string FileName = "recent_files.json";
        private const int MaxRecentFiles = 10;

        // Obtiene la ruta: C:\Users\Usuario\AppData\Roaming\VB6VisualMockupDesigner\recent_files.json
        private static string GetFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, AppName);

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return Path.Combine(folder, FileName);
        }

        public static List<RecentFile> LoadRecents()
        {
            string path = GetFilePath();
            if (!File.Exists(path)) return new List<RecentFile>();

            try
            {
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<RecentFile>>(json) ?? new List<RecentFile>();
            }
            catch
            {
                // Si el archivo está corrupto, retornamos lista vacía
                return new List<RecentFile>();
            }
        }

        public static void AddToRecents(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath)) return;

            var list = LoadRecents();
            var fileInfo = new FileInfo(fullPath);

            // 1. Verificar si ya existe para no duplicar (lo removemos para volver a insertarlo al inicio)
            var existing = list.FirstOrDefault(x => x.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                list.Remove(existing);
            }

            // 2. Insertar al inicio (Top de la pila)
            list.Insert(0, new RecentFile
            {
                Name = fileInfo.Name,
                FullPath = fullPath,
                LastAccess = DateTime.Now
            });

            // 3. Limitar el tamaño de la lista
            if (list.Count > MaxRecentFiles)
            {
                list = list.Take(MaxRecentFiles).ToList();
            }

            // 4. Guardar cambios
            Save(list);
        }

        private static void Save(List<RecentFile> list)
        {
            try
            {
                string json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetFilePath(), json);
            }
            catch (Exception ex)
            {
                // Manejo de errores silencioso o log
                System.Diagnostics.Debug.WriteLine($"Error guardando recientes: {ex.Message}");
            }
        }
    }
}
