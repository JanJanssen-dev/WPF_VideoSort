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

        public OperationGroup(string description)
        {
            GroupStartTime = DateTime.Now;
            Description = description;
        }

        public void AddOperation(FileOperation operation)
        {
            Operations.Add(operation);
        }
    }

    public partial class UndoManager : ObservableObject
    {
        private Stack<OperationGroup> undoStack = new();
        private OperationGroup currentGroup;

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

            currentGroup.AddOperation(operation);
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

            foreach (var operation in group.Operations.AsEnumerable().Reverse())
            {
                try
                {
                    if (File.Exists(operation.DestinationPath))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(operation.SourcePath));
                        File.Move(operation.DestinationPath, operation.SourcePath);
                        successful++;
                    }
                    else
                    {
                        failed++;
                    }
                }
                catch
                {
                    failed++;
                }
            }

            UpdateProperties();
            return (successful, failed);
        }

        public void Clear()
        {
            undoStack.Clear();
            currentGroup = null;
            UpdateProperties();
        }
    }
}