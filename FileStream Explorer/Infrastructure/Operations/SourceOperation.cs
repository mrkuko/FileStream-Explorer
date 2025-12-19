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
    /// Configuration for source operations
    /// </summary>
    public class SourceConfiguration
    {
        /// <summary>
        /// Directory path to load files from
        /// </summary>
        public string SourceDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Whether to include subdirectories
        /// </summary>
        public bool IncludeSubdirectories { get; set; }

        /// <summary>
        /// Whether to merge with existing files in stream (true) or replace them (false)
        /// </summary>
        public bool MergeWithExisting { get; set; } = true;

        /// <summary>
        /// File pattern filter (e.g., "*.txt")
        /// </summary>
        public string FilePattern { get; set; } = "*.*";
    }

    /// <summary>
    /// Source operation that loads files from a folder into the pipeline stream.
    /// Can be used as a starting node or added later to merge additional files.
    /// </summary>
    public class SourceOperation : Core.Operations.FileOperationBase, IConfigurableOperation
    {
        public override string OperationId => "source";
        public override string DisplayName => "Load Files";
        public override string Description => "Load files from a folder into the pipeline";

        private SourceConfiguration _config;

        public object Configuration
        {
            get => _config;
            set => _config = value as SourceConfiguration ?? new SourceConfiguration();
        }

        /// <summary>
        /// Files that were passed to this operation (for merge mode)
        /// </summary>
        private List<FileItem>? _inputFiles;

        public SourceOperation(IOperationContext context) : base(context)
        {
            _config = new SourceConfiguration();
        }

        public SourceOperation(IOperationContext context, SourceConfiguration config) : base(context)
        {
            _config = config ?? new SourceConfiguration();
        }

        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(_config.SourceDirectory))
            {
                result.AddError("Source directory is required");
            }
            else if (!Directory.Exists(_config.SourceDirectory))
            {
                result.AddError($"Source directory does not exist: {_config.SourceDirectory}");
            }

            return result;
        }

        protected override async Task<ValidationResult> ValidateSpecificAsync(List<FileItem> files, CancellationToken cancellationToken)
        {
            _inputFiles = files;
            return await Task.FromResult(ValidateConfiguration());
        }

        protected override async Task<OperationResult> ExecuteSpecificAsync(List<FileItem> files, CancellationToken cancellationToken)
        {
            _inputFiles = files;

            return await Task.Run(() =>
            {
                var result = new OperationResult { Success = true };

                try
                {
                    // Load files from source directory
                    var loadedFiles = LoadFilesFromDirectory();

                    // Track loaded files as changes for preview
                    foreach (var file in loadedFiles)
                    {
                        result.AddChange(new FileChange(file.FullPath, file.FullPath, ChangeType.None)
                        {
                            Description = _config.MergeWithExisting ? "Added to stream" : "Loaded from source",
                            Applied = true
                        });
                    }

                    // If merging, also include original files that aren't duplicates
                    if (_config.MergeWithExisting && _inputFiles != null && _inputFiles.Any())
                    {
                        var loadedPaths = loadedFiles.Select(f => f.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                        foreach (var existingFile in _inputFiles.Where(f => !loadedPaths.Contains(f.FullPath)))
                        {
                            result.AddChange(new FileChange(existingFile.FullPath, existingFile.FullPath, ChangeType.None)
                            {
                                Description = "Kept from stream",
                                Applied = true
                            });
                        }
                    }

                    result.Message = _config.MergeWithExisting
                        ? $"Merged {loadedFiles.Count} files with {_inputFiles?.Count ?? 0} existing files"
                        : $"Loaded {loadedFiles.Count} files from source";
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"Failed to load files: {ex.Message}";
                    result.AddError(ex.Message);
                }

                return result;
            });
        }

        /// <summary>
        /// Gets the files that should be passed to the next operation in the pipeline.
        /// This is called by ProcessingPipeline to update the file list after this operation.
        /// </summary>
        public List<FileItem> GetOutputFiles()
        {
            var loadedFiles = LoadFilesFromDirectory();

            if (_config.MergeWithExisting && _inputFiles != null && _inputFiles.Any())
            {
                // Merge: keep existing files and add new ones (avoid duplicates)
                var existingPaths = _inputFiles.Select(f => f.FullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var newFiles = loadedFiles.Where(f => !existingPaths.Contains(f.FullPath)).ToList();
                return _inputFiles.Concat(newFiles).ToList();
            }

            return loadedFiles;
        }

        private List<FileItem> LoadFilesFromDirectory()
        {
            var result = new List<FileItem>();

            if (string.IsNullOrWhiteSpace(_config.SourceDirectory) || !Directory.Exists(_config.SourceDirectory))
                return result;

            try
            {
                var searchOption = _config.IncludeSubdirectories
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                var pattern = string.IsNullOrWhiteSpace(_config.FilePattern) ? "*.*" : _config.FilePattern;

                var files = Directory.GetFiles(_config.SourceDirectory, pattern, searchOption);

                foreach (var filePath in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        var fileItem = new FileItem(fileInfo.FullName)
                        {
                            Size = fileInfo.Length,
                            ModifiedDate = fileInfo.LastWriteTime,
                            CreatedDate = fileInfo.CreationTime,
                            IsDirectory = false,
                            Attributes = fileInfo.Attributes
                        };
                        result.Add(fileItem);
                    }
                    catch
                    {
                        // Skip files we can't access
                    }
                }
            }
            catch
            {
                // Return empty list if directory access fails
            }

            return result;
        }

        public override IFileOperation Clone()
        {
            return new SourceOperation(Context, new SourceConfiguration
            {
                SourceDirectory = _config.SourceDirectory,
                IncludeSubdirectories = _config.IncludeSubdirectories,
                MergeWithExisting = _config.MergeWithExisting,
                FilePattern = _config.FilePattern
            });
        }
    }
}
