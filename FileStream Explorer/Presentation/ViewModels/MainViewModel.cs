using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Models;
using FileStreamExplorer.Core.Operations;
using FileStreamExplorer.Presentation.Commands;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FileStreamExplorer.Presentation.ViewModels
{
    /// <summary>
    /// Main ViewModel for the file explorer interface
    /// Implements MVVM pattern with proper data binding
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IFileSystemService _fileSystem;
        private readonly IProcessingPipeline _pipeline;
        private readonly OperationFactory _operationFactory;

        private string _currentDirectory = string.Empty;
        private FileItem? _selectedFile;
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private bool _isPreviewMode;

        public ObservableCollection<FileItem> Files { get; }
        public ObservableCollection<FileItem> SelectedFiles { get; }
        public ObservableCollection<FileChange> PreviewChanges { get; }
        public ObservableCollection<string> DirectoryHistory { get; }
        public ObservableCollection<PipelineOperationItem> PipelineOperations { get; }

        /// <summary>
        /// Reference to the pipeline for external access (e.g., adding operations from code-behind)
        /// </summary>
        public IProcessingPipeline Pipeline => _pipeline;

        public string CurrentDirectory
        {
            get => _currentDirectory;
            set
            {
                if (SetProperty(ref _currentDirectory, value))
                {
                    _ = LoadDirectoryAsync(value);
                }
            }
        }

        public FileItem? SelectedFile
        {
            get => _selectedFile;
            set => SetProperty(ref _selectedFile, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsPreviewMode
        {
            get => _isPreviewMode;
            set => SetProperty(ref _isPreviewMode, value);
        }

        // Commands
        public ICommand NavigateUpCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand ExecutePipelineCommand { get; }
        public ICommand PreviewPipelineCommand { get; }
        public ICommand ClearSelectionCommand { get; }

        public MainViewModel(
            IFileSystemService fileSystem,
            IProcessingPipeline pipeline,
            IOperationContext context)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _operationFactory = new OperationFactory(context);

            Files = new ObservableCollection<FileItem>();
            SelectedFiles = new ObservableCollection<FileItem>();
            PreviewChanges = new ObservableCollection<FileChange>();
            DirectoryHistory = new ObservableCollection<string>();
            PipelineOperations = new ObservableCollection<PipelineOperationItem>();

            // Initialize commands
            NavigateUpCommand = new RelayCommand(NavigateUp, CanNavigateUp);
            RefreshCommand = new RelayCommand(async () => await LoadDirectoryAsync(CurrentDirectory));
            OpenFolderCommand = new RelayCommand<FileItem>(OpenFolder, CanOpenFolder);
            ExecutePipelineCommand = new RelayCommand(async () => await ExecutePipelineAsync(), CanExecutePipeline);
            PreviewPipelineCommand = new RelayCommand(async () => await PreviewPipelineAsync(), CanExecutePipeline);
            ClearSelectionCommand = new RelayCommand(ClearSelection);

            // Set initial directory
            CurrentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private async Task LoadDirectoryAsync(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                StatusMessage = "Invalid directory";
                return;
            }

            IsLoading = true;
            StatusMessage = $"Loading {directory}...";

            try
            {
                var items = await _fileSystem.GetFilesAsync(directory, includeSubdirectories: false);
                
                Files.Clear();
                foreach (var item in items.OrderBy(f => !f.IsDirectory).ThenBy(f => f.Name))
                {
                    Files.Add(item);
                }

                if (!DirectoryHistory.Contains(directory))
                {
                    DirectoryHistory.Add(directory);
                }

                StatusMessage = $"Loaded {Files.Count} items";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading directory: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateUp()
        {
            if (!string.IsNullOrEmpty(CurrentDirectory))
            {
                var parent = Directory.GetParent(CurrentDirectory);
                if (parent != null)
                {
                    CurrentDirectory = parent.FullName;
                }
            }
        }

        private bool CanNavigateUp()
        {
            return !string.IsNullOrEmpty(CurrentDirectory) && Directory.GetParent(CurrentDirectory) != null;
        }

        private void OpenFolder(FileItem? folder)
        {
            if (folder != null && folder.IsDirectory)
            {
                CurrentDirectory = folder.FullPath;
            }
        }

        private bool CanOpenFolder(FileItem? folder)
        {
            return folder != null && folder.IsDirectory;
        }

        private async Task ExecutePipelineAsync()
        {
            if (!SelectedFiles.Any())
            {
                StatusMessage = "No files selected";
                return;
            }

            IsLoading = true;
            IsPreviewMode = false;
            StatusMessage = "Executing pipeline...";
            PreviewChanges.Clear();

            try
            {
                var result = await _pipeline.ExecuteAsync(SelectedFiles.ToList());

                if (result.Success)
                {
                    StatusMessage = result.Summary;
                    await LoadDirectoryAsync(CurrentDirectory); // Refresh
                }
                else
                {
                    StatusMessage = $"Pipeline failed: {result.Summary}";
                }

                // Show all changes
                foreach (var stepResult in result.StepResults)
                {
                    foreach (var change in stepResult.Result.Changes)
                    {
                        PreviewChanges.Add(change);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PreviewPipelineAsync()
        {
            if (!SelectedFiles.Any())
            {
                StatusMessage = "No files selected";
                return;
            }

            IsLoading = true;
            IsPreviewMode = true;
            StatusMessage = "Generating preview...";
            PreviewChanges.Clear();

            try
            {
                var result = await _pipeline.PreviewAsync(SelectedFiles.ToList());

                StatusMessage = result.Summary;

                // Show all preview changes
                foreach (var stepResult in result.StepResults)
                {
                    foreach (var change in stepResult.Result.Changes)
                    {
                        PreviewChanges.Add(change);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanExecutePipeline()
        {
            return SelectedFiles.Any() && _pipeline.Operations.Any() && !IsLoading;
        }

        private void ClearSelection()
        {
            SelectedFiles.Clear();
            PreviewChanges.Clear();
            StatusMessage = "Selection cleared";
        }

        public void AddToSelection(FileItem file)
        {
            if (file != null && !SelectedFiles.Contains(file))
            {
                SelectedFiles.Add(file);
                StatusMessage = $"{SelectedFiles.Count} files selected";
            }
        }

        public void RemoveFromSelection(FileItem file)
        {
            if (file != null && SelectedFiles.Contains(file))
            {
                SelectedFiles.Remove(file);
                StatusMessage = $"{SelectedFiles.Count} files selected";
            }
        }

        /// <summary>
        /// Adds an operation to the pipeline and updates the UI collection
        /// </summary>
        public void AddPipelineOperation(IFileOperation operation)
        {
            _pipeline.AddOperation(operation);
            RefreshPipelineOperations();
            StatusMessage = $"Added {operation.DisplayName} to pipeline ({_pipeline.Operations.Count} operations)";
        }

        /// <summary>
        /// Removes an operation from the pipeline and updates the UI collection
        /// </summary>
        public void RemovePipelineOperation(PipelineOperationItem item)
        {
            if (item?.Operation != null)
            {
                _pipeline.RemoveOperation(item.Operation);
                RefreshPipelineOperations();
                StatusMessage = $"Removed {item.DisplayName} from pipeline ({_pipeline.Operations.Count} operations)";
            }
        }

        /// <summary>
        /// Duplicate an existing pipeline operation (inserts the clone after the original)
        /// </summary>
        public void DuplicatePipelineOperation(PipelineOperationItem item)
        {
            if (item?.Operation == null) return;

            // Find index of original
            var ops = _pipeline.Operations;
            int index = -1;
            for (int i = 0; i < ops.Count; i++)
            {
                if (ops[i] == item.Operation)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0) return;

            var clone = item.Operation.Clone();
            // Insert after original
            _pipeline.InsertOperation(index + 1, clone);
            RefreshPipelineOperations();
            StatusMessage = $"Duplicated {item.DisplayName} in pipeline";
        }

        /// <summary>
        /// Moves an operation within the pipeline (for drag-drop reordering)
        /// </summary>
        public void MovePipelineOperation(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= _pipeline.Operations.Count ||
                newIndex < 0 || newIndex >= _pipeline.Operations.Count ||
                oldIndex == newIndex)
                return;

            // Get the operation to move
            var operation = _pipeline.Operations[oldIndex];
            
            // Remove from old position
            _pipeline.RemoveOperation(operation);
            
            // Insert at new position
            _pipeline.InsertOperation(newIndex, operation);
            
            RefreshPipelineOperations();
            StatusMessage = $"Reordered pipeline operations";
        }

        /// <summary>
        /// Clears all operations from the pipeline
        /// </summary>
        public void ClearPipelineOperations()
        {
            _pipeline.Clear();
            RefreshPipelineOperations();
            StatusMessage = "Pipeline cleared";
        }

        /// <summary>
        /// Syncs the PipelineOperations collection with the actual pipeline
        /// </summary>
        public void RefreshPipelineOperations()
        {
            PipelineOperations.Clear();
            for (int i = 0; i < _pipeline.Operations.Count; i++)
            {
                PipelineOperations.Add(new PipelineOperationItem(_pipeline.Operations[i], i + 1));
            }
        }
    }
}
