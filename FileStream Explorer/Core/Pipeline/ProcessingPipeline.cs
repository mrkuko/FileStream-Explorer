using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStreamExplorer.Core.Pipeline
{
    /// <summary>
    /// Processes multiple file operations in sequence
    /// Implements Pipeline pattern for chaining operations
    /// </summary>
    public class ProcessingPipeline : IProcessingPipeline
    {
        private readonly List<IFileOperation> _operations;
        private readonly IOperationContext _context;

        public IReadOnlyList<IFileOperation> Operations => _operations.AsReadOnly();

        public ProcessingPipeline(IOperationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _operations = new List<IFileOperation>();
        }

        public void AddOperation(IFileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            _operations.Add(operation);
        }

        public void InsertOperation(int index, IFileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (index < 0 || index > _operations.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _operations.Insert(index, operation);
        }

        public bool RemoveOperation(IFileOperation operation)
        {
            return _operations.Remove(operation);
        }

        public void Clear()
        {
            _operations.Clear();
        }

        public async Task<ValidationResult> ValidateAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default)
        {
            var result = new ValidationResult { IsValid = true };

            if (!_operations.Any())
            {
                result.AddWarning("No operations in pipeline");
                return result;
            }

            // Validate each operation in sequence
            foreach (var operation in _operations)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var operationValidation = await operation.ValidateAsync(files, cancellationToken);
                result.Merge(operationValidation);

                if (!operationValidation.IsValid && _context.StopOnError)
                    break;
            }

            return result;
        }

        public async Task<PipelineResult> PreviewAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default)
        {
            var previousMode = _context.IsPreviewMode;
            try
            {
                _context.IsPreviewMode = true;
                return await ExecuteAsync(files, cancellationToken);
            }
            finally
            {
                _context.IsPreviewMode = previousMode;
            }
        }

        public async Task<PipelineResult> ExecuteAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default)
        {
            var pipelineResult = new PipelineResult { Success = true };
            var currentFiles = files?.ToList() ?? new List<FileItem>();

            if (!_operations.Any())
            {
                pipelineResult.Success = false;
                pipelineResult.Summary = "No operations to execute";
                return pipelineResult;
            }

            // Execute each operation in sequence
            for (int i = 0; i < _operations.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    pipelineResult.Success = false;
                    pipelineResult.Summary = "Pipeline execution cancelled";
                    break;
                }

                var operation = _operations[i];
                var stepNumber = i + 1;

                try
                {
                    // Execute the operation
                    var operationResult = await operation.ExecuteAsync(currentFiles, cancellationToken);
                    pipelineResult.AddStepResult(stepNumber, operation.DisplayName, operationResult);

                    if (!operationResult.Success)
                    {
                        pipelineResult.Success = false;
                        
                        if (_context.StopOnError)
                        {
                            pipelineResult.Summary = $"Pipeline stopped at step {stepNumber} due to error";
                            break;
                        }
                    }

                    // Update current files based on changes (for next operation in pipeline)
                    currentFiles = UpdateFileList(currentFiles, operationResult.Changes);
                }
                catch (Exception ex)
                {
                    pipelineResult.Success = false;
                    pipelineResult.AddStepResult(stepNumber, operation.DisplayName, 
                        OperationResult.FailureResult($"Exception in step {stepNumber}: {ex.Message}", ex));

                    if (_context.StopOnError)
                    {
                        pipelineResult.Summary = $"Pipeline stopped at step {stepNumber} due to exception";
                        break;
                    }
                }
            }

            // Generate summary
            if (pipelineResult.Success)
            {
                pipelineResult.Summary = _context.IsPreviewMode
                    ? $"Preview completed: {pipelineResult.TotalProcessed} files would be processed"
                    : $"Pipeline completed: {pipelineResult.TotalProcessed} files processed, {pipelineResult.TotalFailed} failed";
            }

            return pipelineResult;
        }

        /// <summary>
        /// Updates the file list based on changes from an operation
        /// This allows subsequent operations to work with modified file paths
        /// </summary>
        private List<FileItem> UpdateFileList(List<FileItem> currentFiles, List<FileChange> changes)
        {
            if (!changes.Any() || _context.IsPreviewMode)
                return currentFiles;

            var updatedFiles = new List<FileItem>();
            var changeMap = changes.Where(c => c.Applied).ToDictionary(c => c.OriginalPath, c => c.NewPath);

            foreach (var file in currentFiles)
            {
                if (changeMap.TryGetValue(file.FullPath, out var newPath))
                {
                    // File was modified, create new FileItem with updated path
                    var updatedFile = file.Clone();
                    updatedFile.FullPath = newPath;
                    updatedFile.Name = System.IO.Path.GetFileName(newPath);
                    updatedFiles.Add(updatedFile);
                }
                else
                {
                    // File unchanged
                    updatedFiles.Add(file);
                }
            }

            return updatedFiles;
        }
    }
}
