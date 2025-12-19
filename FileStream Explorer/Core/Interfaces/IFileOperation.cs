using FileStreamExplorer.Core.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FileStreamExplorer.Core.Interfaces
{
    /// <summary>
    /// Base interface for all file operations
    /// Implements Strategy pattern for extensible operation types
    /// </summary>
    public interface IFileOperation
    {
        /// <summary>
        /// Unique identifier for the operation type
        /// </summary>
        string OperationId { get; }

        /// <summary>
        /// Human-readable name for the operation
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Description of what the operation does
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Validates the operation can be performed on the given files
        /// </summary>
        Task<ValidationResult> ValidateAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default);

        /// <summary>
        /// Previews the changes without executing them
        /// </summary>
        Task<OperationResult> PreviewAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the operation on the given files
        /// </summary>
        Task<OperationResult> ExecuteAsync(IEnumerable<FileItem> files, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a deep copy of the operation with its configuration
        /// </summary>
        IFileOperation Clone();
    }

    /// <summary>
    /// Base interface for operations that can be configured
    /// </summary>
    public interface IConfigurableOperation : IFileOperation
    {
        /// <summary>
        /// Configuration data for the operation
        /// </summary>
        object Configuration { get; set; }

        /// <summary>
        /// Validates the operation configuration
        /// </summary>
        ValidationResult ValidateConfiguration();
    }
}
