using FileStreamExplorer.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileStreamExplorer.Core.Interfaces
{
    /// <summary>
    /// Interface for validating file operations
    /// </summary>
    public interface IFileValidator
    {
        /// <summary>
        /// Validates a single file item
        /// </summary>
        ValidationResult Validate(FileItem file);

        /// <summary>
        /// Validates multiple file items
        /// </summary>
        ValidationResult ValidateMany(IEnumerable<FileItem> files);

        /// <summary>
        /// Validates a file path
        /// </summary>
        ValidationResult ValidatePath(string path);

        /// <summary>
        /// Checks for name collisions in a directory
        /// </summary>
        Task<ValidationResult> ValidateNoCollisionsAsync(IEnumerable<FileChange> changes);
    }

    /// <summary>
    /// Interface for file system operations
    /// </summary>
    public interface IFileSystemService
    {
        /// <summary>
        /// Gets files in the specified directory
        /// </summary>
        Task<IEnumerable<FileItem>> GetFilesAsync(string directory, bool includeSubdirectories = false);

        /// <summary>
        /// Gets detailed information about a file
        /// </summary>
        Task<FileItem> GetFileInfoAsync(string path);

        /// <summary>
        /// Checks if a file or directory exists
        /// </summary>
        bool Exists(string path);

        /// <summary>
        /// Renames a file
        /// </summary>
        Task<bool> RenameFileAsync(string sourcePath, string newName);

        /// <summary>
        /// Moves a file to a new location
        /// </summary>
        Task<bool> MoveFileAsync(string sourcePath, string destinationPath);

        /// <summary>
        /// Deletes a file
        /// </summary>
        Task<bool> DeleteFileAsync(string path);

        /// <summary>
        /// Creates a directory if it doesn't exist
        /// </summary>
        Task<bool> CreateDirectoryAsync(string path);
    }

    /// <summary>
    /// Context information for operation execution
    /// </summary>
    public interface IOperationContext
    {
        /// <summary>
        /// File system service for performing operations
        /// </summary>
        IFileSystemService FileSystem { get; }

        /// <summary>
        /// Validator for checking operation validity
        /// </summary>
        IFileValidator Validator { get; }

        /// <summary>
        /// Whether to execute in preview mode
        /// </summary>
        bool IsPreviewMode { get; set; }

        /// <summary>
        /// Whether to stop on first error
        /// </summary>
        bool StopOnError { get; set; }
    }
}
