using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FileStreamExplorer.Infrastructure.Operations
{
    /// <summary>
    /// Configuration for rename operations
    /// </summary>
    public class RenameConfiguration
    {
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        // When true, treat FindText as a regular expression and use Regex.Replace
        public bool UseRegex { get; set; }
        public bool UseSequentialNumbering { get; set; }
        public int StartNumber { get; set; } = 1;
        public int NumberPadding { get; set; } = 3;
        public bool PreserveExtension { get; set; } = true;
        public string FindText { get; set; } = string.Empty;
        public string ReplaceText { get; set; } = string.Empty;
        public bool NormalizeSpaces { get; set; }
        // When true, keep the original core file name (name before extension) as part of the new name.
        // When false, the core is removed and only prefix/numbering/suffix are used.
        public bool KeepCoreName { get; set; } = true;
        public CaseTransform CaseTransform { get; set; } = CaseTransform.None;
    }

    public enum CaseTransform
    {
        None,
        Uppercase,
        Lowercase,
        TitleCase
    }

    /// <summary>
    /// Rename operation with template-based renaming
    /// </summary>
    public class RenameOperation : Core.Operations.FileOperationBase, IConfigurableOperation
    {
        public override string OperationId => "rename";
        public override string DisplayName => "Rename Files";
        public override string Description => "Rename files using templates and patterns";

        private RenameConfiguration _config;

        public object Configuration
        {
            get => _config;
            set => _config = value as RenameConfiguration ?? new RenameConfiguration();
        }

        public RenameOperation(IOperationContext context) : base(context)
        {
            _config = new RenameConfiguration();
        }

        public RenameOperation(IOperationContext context, RenameConfiguration config) : base(context)
        {
            _config = config ?? new RenameConfiguration();
        }

        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult { IsValid = true };

            // Validate prefix/suffix don't contain invalid characters
            if (!string.IsNullOrEmpty(_config.Prefix))
            {
                var prefixValidation = Context.Validator.ValidateFileName(_config.Prefix + "test.txt");
                if (!prefixValidation.IsValid)
                {
                    result.AddError($"Prefix contains invalid characters: {_config.Prefix}");
                }
            }

            if (!string.IsNullOrEmpty(_config.Suffix))
            {
                var suffixValidation = Context.Validator.ValidateFileName("test" + _config.Suffix + ".txt");
                if (!suffixValidation.IsValid)
                {
                    result.AddError($"Suffix contains invalid characters: {_config.Suffix}");
                }
            }

            if (_config.UseSequentialNumbering && _config.StartNumber < 0)
            {
                result.AddError("Start number must be non-negative");
            }

            if (_config.UseSequentialNumbering && _config.NumberPadding < 1)
            {
                result.AddError("Number padding must be at least 1");
            }

            // Validate regex if requested
            if (_config.UseRegex && !string.IsNullOrEmpty(_config.FindText))
            {
                try
                {
                    _ = new Regex(_config.FindText);
                }
                catch (ArgumentException)
                {
                    result.AddError($"Invalid regex pattern: {_config.FindText}");
                }
            }

            return result;
        }

        protected override async Task<ValidationResult> ValidateSpecificAsync(List<FileItem> files, CancellationToken cancellationToken)
        {
            var result = ValidateConfiguration();

            // Generate preview changes to check for collisions
            var changes = GenerateChanges(files);
            var collisionCheck = await Context.Validator.ValidateNoCollisionsAsync(changes);
            result.Merge(collisionCheck);

            return result;
        }

        protected override async Task<OperationResult> ExecuteSpecificAsync(List<FileItem> files, CancellationToken cancellationToken)
        {
            var result = new OperationResult { Success = true };
            var changes = GenerateChanges(files);

            foreach (var change in changes)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    if (!Context.IsPreviewMode)
                    {
                        var newName = Path.GetFileName(change.NewPath);
                        var success = await Context.FileSystem.RenameFileAsync(change.OriginalPath, newName);
                        change.Applied = success;

                        if (success)
                        {
                            result.AddChange(change);
                        }
                        else
                        {
                            result.AddError($"Failed to rename: {change.OriginalPath}");
                        }
                    }
                    else
                    {
                        // Preview mode - just record the change
                        result.AddChange(change);
                    }
                }
                catch (Exception ex)
                {
                    result.AddError($"Error renaming {change.OriginalPath}: {ex.Message}");
                }
            }

            result.Success = result.FailedCount == 0;
            return result;
        }

        private List<FileChange> GenerateChanges(List<FileItem> files)
        {
            var changes = new List<FileChange>();
            int currentNumber = _config.StartNumber;

            foreach (var file in files.OrderBy(f => f.Name))
            {
                var originalName = file.Name;
                var extension = _config.PreserveExtension ? file.Extension : string.Empty;
                var nameWithoutExt = _config.PreserveExtension 
                    ? Path.GetFileNameWithoutExtension(originalName)
                    : originalName;

                // Decide whether to keep the original core name or start from empty
                var baseName = _config.KeepCoreName ? nameWithoutExt : string.Empty;

                // Apply transformations
                var newName = ApplyTransformations(baseName, currentNumber);
                newName += extension;

                var directory = Path.GetDirectoryName(file.FullPath) ?? string.Empty;
                var newPath = Path.Combine(directory, newName);

                changes.Add(new FileChange(file.FullPath, newPath, ChangeType.Rename)
                {
                    Description = $"{originalName} → {newName}"
                });

                if (_config.UseSequentialNumbering)
                    currentNumber++;
            }

            return changes;
        }

        private string ApplyTransformations(string name, int sequenceNumber)
        {
            var result = name;

            // Find and replace
            if (!string.IsNullOrEmpty(_config.FindText))
            {
                if (_config.UseRegex)
                {
                    try
                    {
                        result = Regex.Replace(result, _config.FindText, _config.ReplaceText ?? string.Empty, RegexOptions.IgnoreCase);
                    }
                    catch (ArgumentException)
                    {
                        // If regex is invalid at runtime, fall back to simple replace
                        result = result.Replace(_config.FindText, _config.ReplaceText ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                    }
                }
                else
                {
                    // Case-insensitive replace for non-regex mode
                    result = result.Replace(_config.FindText, _config.ReplaceText ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                }
            }

            // Normalize spaces
            if (_config.NormalizeSpaces)
            {
                result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();
            }

            // Case transform
            result = _config.CaseTransform switch
            {
                CaseTransform.Uppercase => result.ToUpperInvariant(),
                CaseTransform.Lowercase => result.ToLowerInvariant(),
                CaseTransform.TitleCase => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result.ToLower()),
                _ => result
            };

            // Add prefix
            if (!string.IsNullOrEmpty(_config.Prefix))
            {
                result = _config.Prefix + result;
            }

            // Add sequential number
            if (_config.UseSequentialNumbering)
            {
                var numberStr = sequenceNumber.ToString().PadLeft(_config.NumberPadding, '0');
                result = result + "_" + numberStr;
            }

            // Add suffix
            if (!string.IsNullOrEmpty(_config.Suffix))
            {
                result = result + _config.Suffix;
            }

            return result;
        }

        public override IFileOperation Clone()
        {
            return new RenameOperation(Context, new RenameConfiguration
            {
                Prefix = _config.Prefix,
                Suffix = _config.Suffix,
                UseRegex = _config.UseRegex,
                UseSequentialNumbering = _config.UseSequentialNumbering,
                StartNumber = _config.StartNumber,
                NumberPadding = _config.NumberPadding,
                PreserveExtension = _config.PreserveExtension,
                KeepCoreName = _config.KeepCoreName,
                FindText = _config.FindText,
                ReplaceText = _config.ReplaceText,
                NormalizeSpaces = _config.NormalizeSpaces,
                CaseTransform = _config.CaseTransform
            });
        }
    }
}
