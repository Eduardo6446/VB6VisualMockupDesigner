using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;


namespace VB6VisualMockupDesigner
{
    public enum ExplorerItemType { Project, Folder, File }

    public class ExplorerItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public ExplorerItemType Type { get; set; }
        public ObservableCollection<ExplorerItem> Children { get; set; } = new ObservableCollection<ExplorerItem>();

        public string IconCode
        {
            get
            {
                if (Type == ExplorerItemType.Project) return "\xE82D";
                if (Type == ExplorerItemType.Folder) return "\xE8B7";
                return "\xE8A5";
            }
        }

        public bool IsExpanded { get; set; } = true;
    }

    public static class VbpParser
    {
        public static ExplorerItem ParseProject(string vbpPath)
        {
            // 1. Validación básica para evitar crash por ruta nula o inexistente
            if (string.IsNullOrEmpty(vbpPath) || !File.Exists(vbpPath))
                return null;

            try
            {
                string projectDir = Path.GetDirectoryName(vbpPath);
                // Aseguramos que termine en slash para comparaciones de strings
                if (!projectDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    projectDir += Path.DirectorySeparatorChar;

                string projectName = Path.GetFileNameWithoutExtension(vbpPath);

                var root = new ExplorerItem
                {
                    Name = projectName.ToUpper(),
                    FullPath = vbpPath,
                    Type = ExplorerItemType.Project
                };

                var lines = File.ReadAllLines(vbpPath);
                var files = new List<string>();

                foreach (var line in lines)
                {
                    string path = null;
                    string rawLine = line.Trim();

                    if (rawLine.StartsWith("Form=", StringComparison.OrdinalIgnoreCase))
                    {
                        path = rawLine.Substring(5).Trim();
                    }
                    else if (rawLine.StartsWith("Module=", StringComparison.OrdinalIgnoreCase) ||
                             rawLine.StartsWith("Class=", StringComparison.OrdinalIgnoreCase) ||
                             rawLine.StartsWith("UserControl=", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = rawLine.Split(';');
                        if (parts.Length > 1) path = parts[1].Trim();
                    }

                    if (!string.IsNullOrEmpty(path))
                    {
                        try
                        {
                            // Convertir ruta relativa de VB6 a absoluta
                            string fullPath = Path.GetFullPath(Path.Combine(projectDir, path));
                            files.Add(fullPath);
                        }
                        catch
                        {
                            // Si una ruta específica falla, la ignoramos y seguimos
                            System.Diagnostics.Debug.WriteLine($"Ruta inválida en VBP: {path}");
                        }
                    }
                }

                BuildDirectoryTree(root, files, projectDir);
                return root;
            }
            catch (Exception ex)
            {
                // Capturamos cualquier error general de lectura
                MessageBox.Show($"Error al leer el archivo de proyecto: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private static void BuildDirectoryTree(ExplorerItem root, List<string> filePaths, string baseDir)
        {
            foreach (var filePath in filePaths)
            {
                ExplorerItem currentParent = root;

                // LÓGICA SEGURA:
                // Solo creamos estructura de carpetas si el archivo está DENTRO de la carpeta del proyecto.
                // Si está fuera (ej: C:\Librerias\Common.bas), lo agregamos directo a la raíz para no romper el árbol.
                if (filePath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    // Obtener la parte relativa: "Forms\Login\frmLogin.frm"
                    string relativePath = filePath.Substring(baseDir.Length);
                    string[] parts = relativePath.Split(Path.DirectorySeparatorChar);

                    // Navegar/Crear carpetas (todo menos el último elemento que es el archivo)
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        string folderName = parts[i];
                        if (string.IsNullOrEmpty(folderName)) continue;

                        var folder = currentParent.Children.FirstOrDefault(c => c.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase) && c.Type == ExplorerItemType.Folder);

                        if (folder == null)
                        {
                            folder = new ExplorerItem
                            {
                                Name = folderName,
                                // Ojo: Aquí calculamos la ruta manualmente para evitar errores de Path.Combine con padres
                                FullPath = Path.Combine(currentParent.Type == ExplorerItemType.Project ? baseDir : currentParent.FullPath, folderName),
                                Type = ExplorerItemType.Folder
                            };
                            currentParent.Children.Add(folder);
                        }
                        currentParent = folder;
                    }
                }

                // Agregar el archivo final
                currentParent.Children.Add(new ExplorerItem
                {
                    Name = Path.GetFileName(filePath),
                    FullPath = filePath,
                    Type = ExplorerItemType.File
                });
            }
        }
    }
}
