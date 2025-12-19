using FileStreamExplorer.Core.Interfaces;

namespace FileStreamExplorer.Infrastructure.Services
{
    /// <summary>
    /// Operation context providing dependencies for file operations
    /// </summary>
    public class OperationContext : IOperationContext
    {
        public IFileSystemService FileSystem { get; }
        public IFileValidator Validator { get; }
        public bool IsPreviewMode { get; set; }
        public bool StopOnError { get; set; }

        public OperationContext(IFileSystemService fileSystem, IFileValidator validator)
        {
            FileSystem = fileSystem;
            Validator = validator;
            IsPreviewMode = false;
            StopOnError = true;
        }
    }
}
