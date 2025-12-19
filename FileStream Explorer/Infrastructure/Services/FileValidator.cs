using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileStreamExplorer.Infrastructure.Services
{
    /// <summary>
    /// Implementation of file validation logic
    /// </summary>
    public class FileValidator : IFileValidator
    {
        private readonly IFileSystemService _fileSystem;
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();
        private const int MaxPathLength = 260; // Windows MAX_PATH

        public FileValidator(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public ValidationResult Validate(FileItem file)
        {
            var result = new ValidationResult { IsValid = true };

            if (file == null)
            {
                result.AddError("File item is null", ValidationErrorType.General);
                return result;
            }

            // Check if file exists
            if (!_fileSystem.Exists(file.FullPath))
            {
                result.AddError($"File not found: {file.FullPath}", ValidationErrorType.FileNotFound);
            }

            // Validate path
            var pathValidation = ValidatePath(file.FullPath);
            result.Merge(pathValidation);

            return result;
        }

        public ValidationResult ValidateMany(IEnumerable<FileItem> files)
        {
            var result = new ValidationResult { IsValid = true };
            var fileList = files?.ToList() ?? new List<FileItem>();

            if (!fileList.Any())
            {
                result.AddWarning("No files to validate");
                return result;
            }

            foreach (var file in fileList)
            {
                var fileValidation = Validate(file);
                result.Merge(fileValidation);
            }

            return result;
        }

        public ValidationResult ValidatePath(string path)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(path))
            {
                result.AddError("Path is null or empty", ValidationErrorType.InvalidPath);
                return result;
            }

            // Check for invalid characters
            if (path.IndexOfAny(InvalidPathChars) >= 0)
            {
                result.AddError($"Path contains invalid characters: {path}", ValidationErrorType.InvalidCharacters);
            }

            // Check path length
            if (path.Length > MaxPathLength)
            {
                result.AddError($"Path exceeds maximum length ({MaxPathLength}): {path}", ValidationErrorType.PathTooLong);
            }

            // Validate filename if it's a file path
            var fileName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(fileName))
            {
                var fileNameValidation = ValidateFileName(fileName);
                result.Merge(fileNameValidation);
            }

            return result;
        }

        public async Task<ValidationResult> ValidateNoCollisionsAsync(IEnumerable<FileChange> changes)
        {
            return await Task.Run(() =>
            {
                var result = new ValidationResult { IsValid = true };
                var changeList = changes?.ToList() ?? new List<FileChange>();

                if (!changeList.Any())
                    return result;

                // Group by new path to find duplicates
                var duplicates = changeList
                    .GroupBy(c => c.NewPath, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .ToList();

                foreach (var duplicate in duplicates)
                {
                    result.AddError(
                        $"Multiple files would be renamed to: {duplicate.Key}",
                        ValidationErrorType.NameCollision);
                }

                // Check if new paths already exist in file system
                foreach (var change in changeList)
                {
                    if (change.NewPath != change.OriginalPath && _fileSystem.Exists(change.NewPath))
                    {
                        result.AddError(
                            $"Destination already exists: {change.NewPath}",
                            ValidationErrorType.NameCollision);
                    }
                }

                return result;
            });
        }

        public ValidationResult ValidateFileName(string fileName)
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.AddError("Filename is null or empty", ValidationErrorType.InvalidPath);
                return result;
            }

            // Check for invalid characters
            if (fileName.IndexOfAny(InvalidFileNameChars) >= 0)
            {
                var invalidChars = string.Join(", ", fileName.Where(c => InvalidFileNameChars.Contains(c)).Distinct());
                result.AddError(
                    $"Filename contains invalid characters ({invalidChars}): {fileName}",
                    ValidationErrorType.InvalidCharacters);
            }

            // Check for reserved names (Windows)
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", 
                                       "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", 
                                       "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            if (reservedNames.Contains(nameWithoutExt.ToUpperInvariant()))
            {
                result.AddError($"Filename uses reserved name: {fileName}", ValidationErrorType.InvalidPath);
            }

            return result;
        }
    }
}
