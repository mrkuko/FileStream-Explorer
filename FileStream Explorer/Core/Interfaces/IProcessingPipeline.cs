using FileStreamExplorer.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FileStreamExplorer.Core.Interfaces
{
    /// <summary>
    /// Interface for processing pipelines that execute multiple operations in sequence
    /// Implements Pipeline/Chain of Responsibility pattern
    /// </summary>
    public interface IProcessingPipeline
    {
        /// <summary>
        /// Operations in the pipeline
        /// </summary>
        IReadOnlyList<IFileOperation> Operations { get; }

        /// <summary>
        /// Adds an operation to the end of the pipeline
        /// </summary>
        void AddOperation(IFileOperation operation);

        /// <summary>
        /// Inserts an operation at a specific position
        /// </summary>
        void InsertOperation(int index, IFileOperation operation);

        /// <summary>
        /// Removes an operation from the pipeline
        /// </summary>
        bool RemoveOperation(IFileOperation operation);

        /// <summary>
        /// Clears all operations from the pipeline
        /// </summary>
        void Clear();

        /// <summary>
        /// Validates all operations in the pipeline
        /// </summary>
        Task<ValidationResult> ValidateAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default);

        /// <summary>
        /// Previews all operations in the pipeline
        /// </summary>
        Task<PipelineResult> PreviewAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes all operations in the pipeline sequentially
        /// </summary>
        Task<PipelineResult> ExecuteAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a pipeline execution containing results from all steps
    /// </summary>
    public class PipelineResult
    {
        public bool Success { get; set; }
        public List<StepResult> StepResults { get; set; } = new List<StepResult>();
        public int TotalProcessed { get; set; }
        public int TotalFailed { get; set; }
        public string Summary { get; set; } = string.Empty;

        public void AddStepResult(int stepNumber, string operationName, OperationResult result)
        {
            StepResults.Add(new StepResult
            {
                StepNumber = stepNumber,
                OperationName = operationName,
                Result = result
            });

            TotalProcessed += result.ProcessedCount;
            TotalFailed += result.FailedCount;
        }
    }

    public class StepResult
    {
        public int StepNumber { get; set; }
        public string OperationName { get; set; } = string.Empty;
        public OperationResult Result { get; set; } = new OperationResult();
    }
}
