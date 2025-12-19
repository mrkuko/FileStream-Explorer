using FileStreamExplorer.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace FileStreamExplorer.Core.Operations
{
    /// <summary>
    /// Factory for creating file operation instances
    /// Supports registration of new operation types for extensibility
    /// </summary>
    public class OperationFactory
    {
        private readonly Dictionary<string, Func<IOperationContext, IFileOperation>> _operationCreators;
        private readonly IOperationContext _context;

        public OperationFactory(IOperationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _operationCreators = new Dictionary<string, Func<IOperationContext, IFileOperation>>();
            
            RegisterDefaultOperations();
        }

        /// <summary>
        /// Registers default operations
        /// </summary>
        private void RegisterDefaultOperations()
        {
            // Register built-in operations
            // Additional operations will be registered here as they are implemented
        }

        /// <summary>
        /// Registers a new operation type
        /// </summary>
        public void RegisterOperation(string operationId, Func<IOperationContext, IFileOperation> creator)
        {
            if (string.IsNullOrWhiteSpace(operationId))
                throw new ArgumentException("Operation ID cannot be null or empty", nameof(operationId));

            if (creator == null)
                throw new ArgumentNullException(nameof(creator));

            _operationCreators[operationId] = creator;
        }

        /// <summary>
        /// Creates an operation instance by ID
        /// </summary>
        public IFileOperation CreateOperation(string operationId)
        {
            if (!_operationCreators.ContainsKey(operationId))
                throw new ArgumentException($"Unknown operation ID: {operationId}", nameof(operationId));

            return _operationCreators[operationId](_context);
        }

        /// <summary>
        /// Gets all registered operation IDs
        /// </summary>
        public IEnumerable<string> GetRegisteredOperations()
        {
            return _operationCreators.Keys;
        }

        /// <summary>
        /// Checks if an operation is registered
        /// </summary>
        public bool IsOperationRegistered(string operationId)
        {
            return _operationCreators.ContainsKey(operationId);
        }
    }
}
