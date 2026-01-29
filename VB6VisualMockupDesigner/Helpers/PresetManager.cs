using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json; // Usaremos System.Text.Json (nativo en .NET moderno)

namespace VB6VisualMockupDesigner.Helpers
{
    public class ListPreset
    {
        public string Name { get; set; }
        public string Content { get; set; }
    }

    public static class PresetManager
    {
        private static string _folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VB6VisualMockupDesigner");

        private static string _filePath = Path.Combine(_folderPath, "list_presets.json");

        // Cargar lista desde JSON
        public static List<ListPreset> LoadPresets()
        {
            if (!File.Exists(_filePath)) return new List<ListPreset>();

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<ListPreset>>(json) ?? new List<ListPreset>();
            }
            catch
            {
                return new List<ListPreset>();
            }
        }

        // Guardar un nuevo preset
        public static void SavePreset(string name, string content)
        {
            if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);

            var presets = LoadPresets();

            // Si ya existe uno con ese nombre, lo actualizamos (o podrías lanzar error)
            var existing = presets.Find(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Content = content;
            }
            else
            {
                presets.Add(new ListPreset { Name = name, Content = content });
            }

            string json = JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        // (Opcional) Borrar preset
        public static void DeletePreset(string name)
        {
            var presets = LoadPresets();
            int removed = presets.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                string json = JsonSerializer.Serialize(presets);
                File.WriteAllText(_filePath, json);
            }
        }
    }
}