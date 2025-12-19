using System;
using System.Collections.Generic;
using System.IO;

namespace FileStreamExplorer.Core.Models
{
    /// <summary>
    /// Represents the result of a file operation
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<FileChange> Changes { get; set; } = new List<FileChange>();
        public List<string> Errors { get; set; } = new List<string>();
        public Exception? Exception { get; set; }
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }

        public static OperationResult SuccessResult(string message = "Operation completed successfully")
        {
            return new OperationResult
            {
                Success = true,
                Message = message
            };
        }

        public static OperationResult FailureResult(string message, Exception? ex = null)
        {
            return new OperationResult
            {
                Success = false,
                Message = message,
                Exception = ex
            };
        }

        public void AddChange(FileChange change)
        {
            Changes.Add(change);
            ProcessedCount++;
        }

        public void AddError(string error)
        {
            Errors.Add(error);
            FailedCount++;
        }
    }

    /// <summary>
    /// Represents a single file change
    /// </summary>
    public class FileChange
    {
        public string OriginalPath { get; set; }
        public string NewPath { get; set; }
        public ChangeType Type { get; set; }
        public string? Description { get; set; }
        public bool Applied { get; set; }

        public FileChange(string originalPath, string newPath, ChangeType type)
        {
            OriginalPath = originalPath;
            NewPath = newPath;
            Type = type;
        }

        public override string ToString()
        {
            return Type switch
            {
                ChangeType.Rename => $"Rename: {Path.GetFileName(OriginalPath)} → {Path.GetFileName(NewPath)}",
                ChangeType.Move => $"Move: {OriginalPath} → {NewPath}",
                ChangeType.Delete => $"Delete: {OriginalPath}",
                ChangeType.Modify => $"Modify: {OriginalPath}",
                _ => $"{Type}: {OriginalPath}"
            };
        }
    }

    public enum ChangeType
    {
        None,
        Rename,
        Move,
        Delete,
        Modify,
        Create
    }
}
