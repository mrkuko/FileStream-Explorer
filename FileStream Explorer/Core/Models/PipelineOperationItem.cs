using FileStreamExplorer.Core.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileStreamExplorer.Core.Models
{
    /// <summary>
    /// Wrapper for IFileOperation for display in the pipeline queue UI.
    /// Supports data binding and provides display properties.
    /// </summary>
    public class PipelineOperationItem : INotifyPropertyChanged
    {
        private int _order;
        private IFileOperation _operation;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The order/position of this operation in the pipeline (1-based for display)
        /// </summary>
        public int Order
        {
            get => _order;
            set
            {
                if (_order != value)
                {
                    _order = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The underlying file operation
        /// </summary>
        public IFileOperation Operation
        {
            get => _operation;
            set
            {
                if (_operation != value)
                {
                    _operation = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                    OnPropertyChanged(nameof(Description));
                    OnPropertyChanged(nameof(OperationId));
                }
            }
        }

        /// <summary>
        /// Display name of the operation
        /// </summary>
        public string DisplayName => _operation?.DisplayName ?? "Unknown";

        /// <summary>
        /// Description of the operation
        /// </summary>
        public string Description => _operation?.Description ?? string.Empty;

        /// <summary>
        /// Operation type identifier
        /// </summary>
        public string OperationId => _operation?.OperationId ?? string.Empty;

        /// <summary>
        /// Icon representation based on operation type
        /// </summary>
        public string Icon => OperationId switch
        {
            "source" => "📥",
            "rename" => "📝",
            "move" => "📂",
            "filter" => "🔍",
            "copy" => "📋",
            "delete" => "🗑️",
            _ => "⚙️"
        };

        public PipelineOperationItem(IFileOperation operation, int order)
        {
            _operation = operation;
            _order = order;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
