using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json; // Usaremos System.Text.Json (nativo en .NET moderno)

namespace VB6VisualMockupDesigner.Helpers
{
    /// <summary>
    /// Represents a saved list preset with a name and content.
    /// </summary>
    public class ListPreset
    {
        /// <summary>
        /// Gets or sets the name of the preset.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the content of the preset.
        /// </summary>
        public string Content { get; set; }
    }

    /// <summary>
    /// Manages list presets, allowing users to save and load preset lists.
    /// </summary>
    public static class PresetManager
    {
        private static string _folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VB6VisualMockupDesigner");

        private static string _filePath = Path.Combine(_folderPath, "list_presets.json");

        /// <summary>
        /// Loads all saved presets from the JSON file.
        /// </summary>
        /// <returns>A list of presets, or an empty list if the file doesn't exist or is corrupted.</returns>
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

        /// <summary>
        /// Saves a new preset or updates an existing one with the same name.
        /// </summary>
        /// <param name="name">The name of the preset.</param>
        /// <param name="content">The content to save in the preset.</param>
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

        /// <summary>
        /// Deletes a preset by name.
        /// </summary>
        /// <param name="name">The name of the preset to delete.</param>
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