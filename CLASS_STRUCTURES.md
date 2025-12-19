# Key Class Structures and Relationships

## Core Architecture Components

### 1. Domain Layer - Interfaces

#### IFileOperation (Strategy Pattern)
```csharp
public interface IFileOperation
{
    string OperationId { get; }
    string DisplayName { get; }
    string Description { get; }
    
    Task<ValidationResult> ValidateAsync(IEnumerable<FileItem> files, CancellationToken ct);
    Task<OperationResult> PreviewAsync(IEnumerable<FileItem> files, CancellationToken ct);
    Task<OperationResult> ExecuteAsync(IEnumerable<FileItem> files, CancellationToken ct);
    IFileOperation Clone();
}
```

**Purpose**: Base contract for all file operations. Enables Strategy pattern.

**Key Features**:
- Validation before execution
- Preview mode support
- Cloneable for pipeline serialization
- Async operation support

#### IProcessingPipeline (Pipeline Pattern)
```csharp
public interface IProcessingPipeline
{
    IReadOnlyList<IFileOperation> Operations { get; }
    
    void AddOperation(IFileOperation operation);
    void InsertOperation(int index, IFileOperation operation);
    bool RemoveOperation(IFileOperation operation);
    void Clear();
    
    Task<ValidationResult> ValidateAsync(IEnumerable<FileItem> files, CancellationToken ct);
    Task<PipelineResult> PreviewAsync(IEnumerable<FileItem> files, CancellationToken ct);
    Task<PipelineResult> ExecuteAsync(IEnumerable<FileItem> files, CancellationToken ct);
}
```

**Purpose**: Manages sequential execution of multiple operations.

**Key Features**:
- Dynamic operation management
- Sequential processing
- Aggregated results
- File list transformation between steps

### 2. Domain Layer - Models

#### FileItem
```csharp
public class FileItem
{
    public string FullPath { get; set; }
    public string Name { get; set; }
    public string Extension { get; set; }
    public long Size { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public bool IsDirectory { get; set; }
    public FileAttributes Attributes { get; set; }
    
    public string ParentDirectory => Path.GetDirectoryName(FullPath) ?? string.Empty;
    public FileItem Clone() { ... }
}
```

**Purpose**: Represents a file or directory in the system.

**Key Features**:
- Complete file metadata
- Cloneable for safe transformations
- Directory support

#### OperationResult
```csharp
public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<FileChange> Changes { get; set; }
    public List<string> Errors { get; set; }
    public Exception Exception { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    
    public void AddChange(FileChange change) { ... }
    public void AddError(string error) { ... }
}

public class FileChange
{
    public string OriginalPath { get; set; }
    public string NewPath { get; set; }
    public ChangeType Type { get; set; }  // Rename, Move, Delete, Modify, Create
    public string Description { get; set; }
    public bool Applied { get; set; }
}
```

**Purpose**: Encapsulates operation results with detailed change tracking.

**Key Features**:
- Success/failure status
- Detailed change log
- Error collection
- Statistics

#### ValidationResult
```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; }
    public List<string> Warnings { get; set; }
    
    public void AddError(string message, ValidationErrorType type) { ... }
    public void AddWarning(string message) { ... }
    public void Merge(ValidationResult other) { ... }
    public string GetErrorSummary() { ... }
}

public enum ValidationErrorType
{
    General, InvalidPath, InvalidCharacters, PathTooLong,
    FileNotFound, AccessDenied, FileInUse, NameCollision, InvalidPattern
}
```

**Purpose**: Comprehensive validation result with categorized errors.

**Key Features**:
- Typed errors
- Warning support
- Mergeable results
- Error summaries

### 3. Infrastructure Layer - Base Classes

#### FileOperationBase (Template Method Pattern)
```csharp
public abstract class FileOperationBase : IFileOperation
{
    protected IOperationContext Context { get; }
    
    public abstract string OperationId { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    
    // Template method - calls validation hooks
    public virtual async Task<ValidationResult> ValidateAsync(
        IEnumerable<FileItem> files, CancellationToken ct)
    {
        var result = new ValidationResult { IsValid = true };
        
        // Common validation
        foreach (var file in files)
            result.Merge(Context.Validator.Validate(file));
        
        // Specific validation (hook)
        result.Merge(await ValidateSpecificAsync(files.ToList(), ct));
        
        return result;
    }
    
    // Template method - handles preview mode
    public async Task<OperationResult> ExecuteAsync(
        IEnumerable<FileItem> files, CancellationToken ct)
    {
        var validation = await ValidateAsync(files, ct);
        if (!validation.IsValid)
            return OperationResult.FailureResult("Validation failed");
        
        // Specific execution (hook)
        return await ExecuteSpecificAsync(files.ToList(), ct);
    }
    
    // Hooks for derived classes
    protected abstract Task<ValidationResult> ValidateSpecificAsync(
        List<FileItem> files, CancellationToken ct);
    protected abstract Task<OperationResult> ExecuteSpecificAsync(
        List<FileItem> files, CancellationToken ct);
}
```

**Purpose**: Base class providing common operation functionality.

**Key Features**:
- Template Method pattern
- Common validation logic
- Preview mode handling
- Error handling

### 4. Infrastructure Layer - Services

#### FileSystemService
```csharp
public class FileSystemService : IFileSystemService
{
    public async Task<IEnumerable<FileItem>> GetFilesAsync(
        string directory, bool includeSubdirectories = false) { ... }
    
    public async Task<FileItem> GetFileInfoAsync(string path) { ... }
    public bool Exists(string path) { ... }
    public async Task<bool> RenameFileAsync(string sourcePath, string newName) { ... }
    public async Task<bool> MoveFileAsync(string sourcePath, string destPath) { ... }
    public async Task<bool> DeleteFileAsync(string path) { ... }
    public async Task<bool> CreateDirectoryAsync(string path) { ... }
}
```

**Purpose**: Abstracts file system operations for testability.

**Key Features**:
- Async operations
- Error handling
- Directory and file support

#### FileValidator
```csharp
public class FileValidator : IFileValidator
{
    private readonly IFileSystemService _fileSystem;
    
    public ValidationResult Validate(FileItem file) { ... }
    public ValidationResult ValidateMany(IEnumerable<FileItem> files) { ... }
    public ValidationResult ValidatePath(string path) { ... }
    public async Task<ValidationResult> ValidateNoCollisionsAsync(
        IEnumerable<FileChange> changes) { ... }
}
```

**Purpose**: Centralized validation logic.

**Key Features**:
- Path validation
- Character validation
- Collision detection
- Reserved name checking

### 5. Infrastructure Layer - Operations

#### RenameOperation (Example Implementation)
```csharp
public class RenameOperation : FileOperationBase, IConfigurableOperation
{
    public override string OperationId => "rename";
    public override string DisplayName => "Rename Files";
    
    private RenameConfiguration _config;
    public object Configuration 
    { 
        get => _config;
        set => _config = value as RenameConfiguration ?? new RenameConfiguration();
    }
    
    protected override async Task<ValidationResult> ValidateSpecificAsync(
        List<FileItem> files, CancellationToken ct)
    {
        var result = ValidateConfiguration();
        var changes = GenerateChanges(files);
        var collisionCheck = await Context.Validator.ValidateNoCollisionsAsync(changes);
        result.Merge(collisionCheck);
        return result;
    }
    
    protected override async Task<OperationResult> ExecuteSpecificAsync(
        List<FileItem> files, CancellationToken ct)
    {
        var result = new OperationResult { Success = true };
        var changes = GenerateChanges(files);
        
        foreach (var change in changes)
        {
            if (!Context.IsPreviewMode)
            {
                var newName = Path.GetFileName(change.NewPath);
                var success = await Context.FileSystem.RenameFileAsync(
                    change.OriginalPath, newName);
                change.Applied = success;
            }
            result.AddChange(change);
        }
        
        return result;
    }
    
    private List<FileChange> GenerateChanges(List<FileItem> files) { ... }
    private string ApplyTransformations(string name, int sequenceNumber) { ... }
}
```

**Purpose**: Implements file renaming with templates.

**Key Features**:
- Template-based renaming
- Sequential numbering
- Case transformations
- Find/replace

### 6. Presentation Layer - ViewModels

#### MainViewModel (MVVM Pattern)
```csharp
public class MainViewModel : ViewModelBase
{
    private readonly IFileSystemService _fileSystem;
    private readonly IProcessingPipeline _pipeline;
    
    // Observable Collections for binding
    public ObservableCollection<FileItem> Files { get; }
    public ObservableCollection<FileItem> SelectedFiles { get; }
    public ObservableCollection<FileChange> PreviewChanges { get; }
    
    // Properties with INotifyPropertyChanged
    private string _currentDirectory;
    public string CurrentDirectory
    {
        get => _currentDirectory;
        set
        {
            if (SetProperty(ref _currentDirectory, value))
                LoadDirectoryAsync(value);
        }
    }
    
    // Commands
    public ICommand NavigateUpCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ExecutePipelineCommand { get; }
    public ICommand PreviewPipelineCommand { get; }
    
    // Command implementations
    private async Task ExecutePipelineAsync()
    {
        IsLoading = true;
        Context.IsPreviewMode = false;
        
        var result = await _pipeline.ExecuteAsync(SelectedFiles.ToList());
        
        StatusMessage = result.Summary;
        PreviewChanges.Clear();
        foreach (var change in result.StepResults.SelectMany(sr => sr.Result.Changes))
            PreviewChanges.Add(change);
        
        IsLoading = false;
        await LoadDirectoryAsync(CurrentDirectory); // Refresh
    }
}
```

**Purpose**: Presentation logic and UI state management.

**Key Features**:
- Property change notification
- Command pattern integration
- Async UI operations
- Collection synchronization

### 7. Presentation Layer - Commands

#### RelayCommand
```csharp
public class RelayCommand : ICommand
{
    private readonly Action<object> _execute;
    private readonly Predicate<object> _canExecute;
    
    public event EventHandler CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
    
    public bool CanExecute(object parameter) => 
        _canExecute == null || _canExecute(parameter);
    
    public void Execute(object parameter) => _execute(parameter);
}
```

**Purpose**: ICommand implementation for MVVM.

**Key Features**:
- Automatic CanExecute refresh
- Lambda support
- Parameter passing

## Component Interactions

### Typical Operation Flow

```
User Action (UI)
    ↓
Command in ViewModel
    ↓
Configure Operation via Dialog
    ↓
Add Operation to Pipeline
    ↓
User Clicks "Preview" or "Execute"
    ↓
Pipeline.ValidateAsync()
    ├─> Operation1.ValidateAsync()
    ├─> Operation2.ValidateAsync()
    └─> Operation3.ValidateAsync()
    ↓
Pipeline.ExecuteAsync() or PreviewAsync()
    ├─> Operation1.ExecuteAsync(files) → changes1
    ├─> Update file list based on changes1
    ├─> Operation2.ExecuteAsync(updated_files) → changes2
    ├─> Update file list based on changes2
    └─> Operation3.ExecuteAsync(updated_files) → changes3
    ↓
PipelineResult with all changes
    ↓
Update ViewModel collections
    ↓
UI updates via data binding
```

### Dependency Flow

```
MainWindow (View)
    ↓ (uses)
MainViewModel
    ↓ (depends on)
IProcessingPipeline + IFileSystemService
    ↓ (uses)
IFileOperation implementations
    ↓ (uses)
IOperationContext (IFileSystemService + IFileValidator)
    ↓ (uses)
FileSystemService (actual file I/O)
```

## Key Design Benefits

### 1. Testability
- All dependencies injected via interfaces
- File system mockable via IFileSystemService
- Operations testable in isolation

### 2. Extensibility
- New operations: implement IFileOperation
- New validators: implement IFileValidator
- New file systems: implement IFileSystemService

### 3. Maintainability
- Clear separation of concerns
- Single responsibility per class
- Well-defined interfaces

### 4. Safety
- Preview mode built into architecture
- Comprehensive validation
- Transaction-like pipeline (can rollback if needed)

### 5. Performance
- Async operations prevent UI freezing
- Cancellation token support
- Parallel operation potential (future)
