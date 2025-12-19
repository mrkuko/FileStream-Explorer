using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStreamExplorer.Core.Operations
{
    /// <summary>
    /// Abstract base class for file operations
    /// Provides common functionality for all operation types
    /// </summary>
    public abstract class FileOperationBase : IFileOperation
    {
        protected IOperationContext Context { get; }

        public abstract string OperationId { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }

        protected FileOperationBase(IOperationContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public virtual async Task<ValidationResult> ValidateAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default)
        {
            var result = new ValidationResult { IsValid = true };
            var fileList = files?.ToList() ?? new List<FileItem>();

            if (!fileList.Any())
            {
                result.AddError("No files selected for operation", ValidationErrorType.General);
                return result;
            }

            // Validate each file exists and is accessible
            foreach (var file in fileList)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var fileValidation = Context.Validator.Validate(file);
                result.Merge(fileValidation);
            }

            // Perform operation-specific validation
            var specificValidation = await ValidateSpecificAsync(fileList, cancellationToken);
            result.Merge(specificValidation);

            return result;
        }

        public async Task<OperationResult> PreviewAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default)
        {
            var previousMode = Context.IsPreviewMode;
            try
            {
                Context.IsPreviewMode = true;
                return await ExecuteAsync(files, cancellationToken);
            }
            finally
            {
                Context.IsPreviewMode = previousMode;
            }
        }

        public async Task<OperationResult> ExecuteAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default)
        {
            var result = new OperationResult();
            var fileList = files?.ToList() ?? new List<FileItem>();

            try
            {
                // Validate before execution
                var validation = await ValidateAsync(fileList, cancellationToken);
                if (!validation.IsValid)
                {
                    result.Success = false;
                    result.Message = "Validation failed";
                    result.Errors.AddRange(validation.Errors.Select(e => e.Message));
                    return result;
                }

                // Execute operation-specific logic
                result = await ExecuteSpecificAsync(fileList, cancellationToken);
                
                if (result.Success)
                {
                    result.Message = Context.IsPreviewMode 
                        ? "Preview completed successfully"
                        : "Operation completed successfully";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Operation failed: {ex.Message}";
                result.Exception = ex;
            }

            return result;
        }

        /// <summary>
        /// Operation-specific validation logic
        /// </summary>
        protected abstract Task<ValidationResult> ValidateSpecificAsync(List<FileItem> files, CancellationToken cancellationToken);

        /// <summary>
        /// Operation-specific execution logic
        /// </summary>
        protected abstract Task<OperationResult> ExecuteSpecificAsync(List<FileItem> files, CancellationToken cancellationToken);

        public abstract IFileOperation Clone();
    }
}
