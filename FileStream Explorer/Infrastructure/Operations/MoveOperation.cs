using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStreamExplorer.Infrastructure.Operations
{
    /// <summary>
    /// Configuration for move operations
    /// </summary>
    public class MoveConfiguration
    {
        public string DestinationDirectory { get; set; } = string.Empty;
        public bool CreateSubdirectoriesByExtension { get; set; }
        public bool CreateSubdirectoriesByDate { get; set; }
        public string DateFormat { get; set; } = "yyyy-MM";
        public bool PreserveFolderStructure { get; set; }
    }

    /// <summary>
    /// Move operation for organizing files into directories
    /// </summary>
    public class MoveOperation : Core.Operations.FileOperationBase, IConfigurableOperation
    {
        public override string OperationId => "move";
        public override string DisplayName => "Move Files";
        public override string Description => "Move files to different locations based on rules";

        private MoveConfiguration _config;

        public object Configuration
        {
            get => _config;
            set => _config = value as MoveConfiguration ?? new MoveConfiguration();
        }

        public MoveOperation(IOperationContext context) : base(context)
        {
            _config = new MoveConfiguration();
        }

        public MoveOperation(IOperationContext context, MoveConfiguration config) : base(context)
        {
            _config = config ?? new MoveConfiguration();
        }

        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(_config.DestinationDirectory))
            {
                result.AddError("Destination directory is required");
                return result;
            }

            var pathValidation = Context.Validator.ValidatePath(_config.DestinationDirectory);
            result.Merge(pathValidation);

            return result;
        }

        protected override async Task<ValidationResult> ValidateSpecificAsync(List<FileItem> files, CancellationToken cancellationToken)
        {
            var result = ValidateConfiguration();

            // Check if destination is valid
            if (!Directory.Exists(_config.DestinationDirectory))
            {
                result.AddWarning($"Destination directory will be created: {_config.DestinationDirectory}");
            }

            // Generate preview changes to check for collisions
            var changes = await GenerateChangesAsync(files);
            var collisionCheck = await Context.Validator.ValidateNoCollisionsAsync(changes);
            result.Merge(collisionCheck);

            return result;
        }

        protected override async Task<OperationResult> ExecuteSpecificAsync(List<FileItem> files, CancellationToken cancellationToken)
        {
            var result = new OperationResult { Success = true };
            var changes = await GenerateChangesAsync(files);

            // Create destination directory if needed
            if (!Context.IsPreviewMode && !Directory.Exists(_config.DestinationDirectory))
            {
                await Context.FileSystem.CreateDirectoryAsync(_config.DestinationDirectory);
            }

            foreach (var change in changes)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    if (!Context.IsPreviewMode)
                    {
                        // Create subdirectories if needed
                        var destDir = Path.GetDirectoryName(change.NewPath);
                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        {
                            await Context.FileSystem.CreateDirectoryAsync(destDir);
                        }

                        var success = await Context.FileSystem.MoveFileAsync(change.OriginalPath, change.NewPath);
                        change.Applied = success;

                        if (success)
                        {
                            result.AddChange(change);
                        }
                        else
                        {
                            result.AddError($"Failed to move: {change.OriginalPath}");
                        }
                    }
                    else
                    {
                        result.AddChange(change);
                    }
                }
                catch (Exception ex)
                {
                    result.AddError($"Error moving {change.OriginalPath}: {ex.Message}");
                }
            }

            result.Success = result.FailedCount == 0;
            return result;
        }

        private async Task<List<FileChange>> GenerateChangesAsync(List<FileItem> files)
        {
            return await Task.Run(() =>
            {
                var changes = new List<FileChange>();

                foreach (var file in files)
                {
                    var subdirectory = DetermineSubdirectory(file);
                    var destinationPath = Path.Combine(_config.DestinationDirectory, subdirectory, file.Name);

                    changes.Add(new FileChange(file.FullPath, destinationPath, ChangeType.Move)
                    {
                        Description = $"Move to {subdirectory}"
                    });
                }

                return changes;
            });
        }

        private string DetermineSubdirectory(FileItem file)
        {
            var parts = new List<string>();

            // Preserve original folder structure
            if (_config.PreserveFolderStructure)
            {
                var relativePath = GetRelativePath(file.FullPath);
                if (!string.IsNullOrEmpty(relativePath))
                {
                    parts.Add(relativePath);
                }
            }

            // Organize by extension
            if (_config.CreateSubdirectoriesByExtension && !string.IsNullOrEmpty(file.Extension))
            {
                parts.Add(file.Extension.TrimStart('.'));
            }

            // Organize by date
            if (_config.CreateSubdirectoriesByDate)
            {
                parts.Add(file.ModifiedDate.ToString(_config.DateFormat));
            }

            return parts.Any() ? Path.Combine(parts.ToArray()) : string.Empty;
        }

        private string GetRelativePath(string fullPath)
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(directory))
                return string.Empty;

            // Get parent directory name
            return new DirectoryInfo(directory).Name;
        }

        public override IFileOperation Clone()
        {
            return new MoveOperation(Context, new MoveConfiguration
            {
                DestinationDirectory = _config.DestinationDirectory,
                CreateSubdirectoriesByExtension = _config.CreateSubdirectoriesByExtension,
                CreateSubdirectoriesByDate = _config.CreateSubdirectoriesByDate,
                DateFormat = _config.DateFormat,
                PreserveFolderStructure = _config.PreserveFolderStructure
            });
        }
    }
}
