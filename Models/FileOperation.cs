using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WPF_VideoSort.Models
{
    public class FileOperation
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public DateTime OperationTime { get; set; }

        public FileOperation(string sourcePath, string destinationPath)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
            OperationTime = DateTime.Now;
        }
    }

    public class OperationGroup
    {
        public List<FileOperation> Operations { get; } = new();
        public DateTime GroupStartTime { get; }
        public string Description { get; }
        public string BaseDestinationFolder { get; set; } = string.Empty;

        public OperationGroup(string description)
        {
            GroupStartTime = DateTime.Now;
            Description = description;
        }

        public void AddOperation(FileOperation operation)
        {
            Operations.Add(operation);

            // Speichere den Basis-Zielordner für späteres Aufräumen
            if (string.IsNullOrEmpty(BaseDestinationFolder) && !string.IsNullOrEmpty(operation.DestinationPath))
            {
                BaseDestinationFolder = Path.GetDirectoryName(operation.DestinationPath) ?? string.Empty;
                while (!string.IsNullOrEmpty(BaseDestinationFolder))
                {
                    var parent = Directory.GetParent(BaseDestinationFolder);
                    if (parent == null) break;

                    // Suche nach dem obersten Ordner der Sortierung (wo die Kategorie-Ordner beginnen)
                    if (Enum.GetNames(typeof(FileCategory)).Any(cat =>
                        cat.Equals(Path.GetFileName(BaseDestinationFolder), StringComparison.OrdinalIgnoreCase)))
                    {
                        break;
                    }
                    BaseDestinationFolder = parent.FullName;
                }
            }
        }
    }

    public partial class UndoManager : ObservableObject
    {
        private Stack<OperationGroup> undoStack = new();
        private OperationGroup? currentGroup;

        [ObservableProperty]
        private bool canUndo;

        [ObservableProperty]
        private int pendingUndoOperations;

        public void StartOperationGroup(string description)
        {
            currentGroup = new OperationGroup(description);
        }

        public void AddOperation(FileOperation operation)
        {
            if (currentGroup == null)
            {
                StartOperationGroup("Einzeloperation");
            }

            currentGroup?.AddOperation(operation);
        }

        public void CommitOperationGroup()
        {
            if (currentGroup != null && currentGroup.Operations.Count > 0)
            {
                undoStack.Push(currentGroup);
                UpdateProperties();
            }
            currentGroup = null;
        }

        private void UpdateProperties()
        {
            CanUndo = undoStack.Count > 0;
            PendingUndoOperations = undoStack.Sum(group => group.Operations.Count);
        }

        public async Task<(int successful, int failed)> UndoLastOperationGroup()
        {
            if (undoStack.Count == 0)
            {
                CanUndo = false;
                PendingUndoOperations = 0;
                return (0, 0);
            }

            var group = undoStack.Pop();
            int successful = 0;
            int failed = 0;

            // Operationen in umgekehrter Reihenfolge ausführen
            foreach (var operation in group.Operations.AsEnumerable().Reverse())
            {
                try
                {
                    // Prüfen ob die Datei am Zielort existiert
                    if (!File.Exists(operation.DestinationPath))
                    {
                        failed++;
                        continue;
                    }

                    // Zielverzeichnis erstellen, falls es nicht existiert
                    var sourceDir = Path.GetDirectoryName(operation.SourcePath);
                    if (!string.IsNullOrEmpty(sourceDir) && !Directory.Exists(sourceDir))
                    {
                        Directory.CreateDirectory(sourceDir);
                    }

                    // Wenn eine Datei am ursprünglichen Ort existiert, diese umbenennen
                    string finalSourcePath = operation.SourcePath;
                    if (File.Exists(operation.SourcePath))
                    {
                        string directory = Path.GetDirectoryName(operation.SourcePath) ?? "";
                        string fileName = Path.GetFileNameWithoutExtension(operation.SourcePath);
                        string extension = Path.GetExtension(operation.SourcePath);
                        int counter = 1;

                        do
                        {
                            finalSourcePath = Path.Combine(directory, $"{fileName}_restored_{counter++}{extension}");
                        } while (File.Exists(finalSourcePath));
                    }

                    // Datei zurück verschieben
                    await Task.Run(() => File.Move(operation.DestinationPath, finalSourcePath));
                    successful++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fehler beim Rückgängigmachen: {ex.Message}");
                    failed++;
                }
            }

            // Aufräumen der leeren Ordner nach dem Zurückverschieben
            if (!string.IsNullOrEmpty(group.BaseDestinationFolder))
            {
                await CleanupEmptyDirectoriesAsync(group.BaseDestinationFolder);
            }

            UpdateProperties();
            return (successful, failed);
        }

        private async Task CleanupEmptyDirectoriesAsync(string startPath)
        {
            if (string.IsNullOrEmpty(startPath) || !Directory.Exists(startPath))
                return;

            try
            {
                await Task.Run(() =>
                {
                    // Von unten nach oben durch die Verzeichnisstruktur gehen
                    foreach (var dir in Directory.GetDirectories(startPath, "*", SearchOption.AllDirectories)
                                               .OrderByDescending(d => d.Length))
                    {
                        try
                        {
                            var dirInfo = new DirectoryInfo(dir);
                            if (IsDirectoryEmpty(dirInfo))
                            {
                                dirInfo.Delete();
                                System.Diagnostics.Debug.WriteLine($"Leerer Ordner gelöscht: {dir}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Fehler beim Löschen des Ordners {dir}: {ex.Message}");
                        }
                    }

                    // Zum Schluss den Startordner prüfen
                    var startDirInfo = new DirectoryInfo(startPath);
                    if (IsDirectoryEmpty(startDirInfo))
                    {
                        startDirInfo.Delete();
                        System.Diagnostics.Debug.WriteLine($"Leerer Ordner gelöscht: {startPath}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fehler beim Aufräumen der Ordner: {ex.Message}");
            }
        }

        private bool IsDirectoryEmpty(DirectoryInfo dirInfo)
        {
            return !dirInfo.GetFiles().Any() && !dirInfo.GetDirectories().Any();
        }

        public void Clear()
        {
            undoStack.Clear();
            currentGroup = null;
            UpdateProperties();
        }
    }
}