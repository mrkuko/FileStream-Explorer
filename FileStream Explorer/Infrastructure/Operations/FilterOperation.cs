using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FileStreamExplorer.Infrastructure.Operations
{
    /// <summary>
    /// Configuration for filter operations
    /// </summary>
    public class FilterConfiguration
    {
        public string NamePattern { get; set; } = string.Empty;
        public bool UseRegex { get; set; }
        public List<string> Extensions { get; set; } = new List<string>();
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
        public bool IncludeDirectories { get; set; } = true;
    }

    /// <summary>
    /// Filter operation to select subset of files based on criteria
    /// Note: This operation doesn't modify files, it filters the collection
    /// </summary>
    public class FilterOperation : Core.Operations.FileOperationBase, IConfigurableOperation
    {
        public override string OperationId => "filter";
        public override string DisplayName => "Filter Files";
        public override string Description => "Filter files based on name, extension, size, or date";

        private FilterConfiguration _config;

        public object Configuration
        {
            get => _config;
            set => _config = value as FilterConfiguration ?? new FilterConfiguration();
        }

        public FilterOperation(IOperationContext context) : base(context)
        {
            _config = new FilterConfiguration();
        }

        public FilterOperation(IOperationContext context, FilterConfiguration config) : base(context)
        {
            _config = config ?? new FilterConfiguration();
        }

        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult { IsValid = true };

            // Validate regex pattern if used
            if (_config.UseRegex && !string.IsNullOrEmpty(_config.NamePattern))
            {
                try
                {
                    _ = new Regex(_config.NamePattern);
                }
                catch (ArgumentException)
                {
                    result.AddError($"Invalid regex pattern: {_config.NamePattern}", ValidationErrorType.InvalidPattern);
                }
            }

            // Validate size range
            if (_config.MinSize.HasValue && _config.MaxSize.HasValue && _config.MinSize > _config.MaxSize)
            {
                result.AddError("Minimum size cannot be greater than maximum size");
            }

            // Validate date range
            if (_config.MinDate.HasValue && _config.MaxDate.HasValue && _config.MinDate > _config.MaxDate)
            {
                result.AddError("Minimum date cannot be later than maximum date");
            }

            return result;
        }

        protected override async Task<ValidationResult> ValidateSpecificAsync(List<FileItem> files, CancellationToken cancellationToken)
        {
            return await Task.FromResult(ValidateConfiguration());
        }

        protected override async Task<OperationResult> ExecuteSpecificAsync(List<FileItem> files, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var result = new OperationResult { Success = true };
                var filteredFiles = ApplyFilters(files);

                // Filter operation doesn't modify files, but we track what passed the filter
                foreach (var file in filteredFiles)
                {
                    result.AddChange(new FileChange(file.FullPath, file.FullPath, ChangeType.Modify)
                    {
                        Description = "Matched filter criteria",
                        Applied = true
                    });
                }

                result.Message = $"Filtered {files.Count} files to {filteredFiles.Count} matches";
                return result;
            });
        }

        private List<FileItem> ApplyFilters(List<FileItem> files)
        {
            var filtered = files.AsEnumerable();

            // Filter by directories
            if (!_config.IncludeDirectories)
            {
                filtered = filtered.Where(f => !f.IsDirectory);
            }

            // Filter by name pattern
            if (!string.IsNullOrEmpty(_config.NamePattern))
            {
                if (_config.UseRegex)
                {
                    var regex = new Regex(_config.NamePattern, RegexOptions.IgnoreCase);
                    filtered = filtered.Where(f => regex.IsMatch(f.Name));
                }
                else
                {
                    filtered = filtered.Where(f => f.Name.Contains(_config.NamePattern, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Filter by extensions
            if (_config.Extensions.Any())
            {
                var extensions = _config.Extensions.Select(e => e.StartsWith(".") ? e : "." + e).ToHashSet(StringComparer.OrdinalIgnoreCase);
                filtered = filtered.Where(f => extensions.Contains(f.Extension));
            }

            // Filter by size
            if (_config.MinSize.HasValue)
            {
                filtered = filtered.Where(f => f.Size >= _config.MinSize.Value);
            }
            if (_config.MaxSize.HasValue)
            {
                filtered = filtered.Where(f => f.Size <= _config.MaxSize.Value);
            }

            // Filter by date
            if (_config.MinDate.HasValue)
            {
                filtered = filtered.Where(f => f.ModifiedDate >= _config.MinDate.Value);
            }
            if (_config.MaxDate.HasValue)
            {
                filtered = filtered.Where(f => f.ModifiedDate <= _config.MaxDate.Value);
            }

            return filtered.ToList();
        }

        public override IFileOperation Clone()
        {
            return new FilterOperation(Context, new FilterConfiguration
            {
                NamePattern = _config.NamePattern,
                UseRegex = _config.UseRegex,
                Extensions = new List<string>(_config.Extensions),
                MinSize = _config.MinSize,
                MaxSize = _config.MaxSize,
                MinDate = _config.MinDate,
                MaxDate = _config.MaxDate,
                IncludeDirectories = _config.IncludeDirectories
            });
        }
    }
}
