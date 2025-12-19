# System Architecture Diagrams

## Component Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         USER INTERFACE                          │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ MainWindow.xaml                                           │ │
│  │  • Navigation Bar (path, up, refresh)                     │ │
│  │  • File Grid (multi-select, sort, double-click)           │ │
│  │  • Operations Panel (rename, move, filter buttons)        │ │
│  │  • Preview Panel (shows changes before execution)         │ │
│  │  • Status Bar (messages, file count)                      │ │
│  └───────────────────────────────────────────────────────────┘ │
│                             ↑↓                                  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ Configuration Dialogs                                      │ │
│  │  • RenameConfigDialog.xaml                                │ │
│  │  • MoveConfigDialog.xaml                                  │ │
│  │  • FilterConfigDialog.xaml                                │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                ↑↓ (Data Binding)
┌─────────────────────────────────────────────────────────────────┐
│                      PRESENTATION LOGIC                         │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ MainViewModel                                              │ │
│  │  • ObservableCollection<FileItem> Files                   │ │
│  │  • ObservableCollection<FileItem> SelectedFiles           │ │
│  │  • ObservableCollection<FileChange> PreviewChanges        │ │
│  │  • ICommand NavigateUpCommand                             │ │
│  │  • ICommand ExecutePipelineCommand                        │ │
│  │  • ICommand PreviewPipelineCommand                        │ │
│  │  • async Task LoadDirectoryAsync()                        │ │
│  │  • async Task ExecutePipelineAsync()                      │ │
│  │  • async Task PreviewPipelineAsync()                      │ │
│  └───────────────────────────────────────────────────────────┘ │
│                             ↑↓                                  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ RelayCommand (ICommand)                                    │ │
│  │  • Execute(Action)                                         │ │
│  │  • CanExecute(Predicate)                                  │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                ↑↓
┌─────────────────────────────────────────────────────────────────┐
│                     APPLICATION LOGIC                           │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ ProcessingPipeline : IProcessingPipeline                  │ │
│  │  • List<IFileOperation> Operations                        │ │
│  │  • AddOperation()                                         │ │
│  │  • async ValidateAsync()                                  │ │
│  │  • async PreviewAsync()  ─┐                              │ │
│  │  • async ExecuteAsync()   │ (sequential)                  │ │
│  │     ├─> Step 1: Filter   │                               │ │
│  │     ├─> Step 2: Rename   │                               │ │
│  │     └─> Step 3: Move     ┘                               │ │
│  └───────────────────────────────────────────────────────────┘ │
│                             ↑↓                                  │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ OperationFactory                                           │ │
│  │  • RegisterOperation(id, creator)                         │ │
│  │  • CreateOperation(id) → IFileOperation                   │ │
│  │  • GetRegisteredOperations()                              │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                ↑↓
┌─────────────────────────────────────────────────────────────────┐
│                       DOMAIN LAYER                              │
│                                                                 │
│  ┌────────────────────┬──────────────────┬────────────────────┐ │
│  │ IFileOperation     │ IFileSystemSvc   │ IProcessingPipeline│ │
│  │  • ValidateAsync() │  • GetFilesAsync │  • Operations      │ │
│  │  • PreviewAsync()  │  • RenameAsync   │  • ExecuteAsync()  │ │
│  │  • ExecuteAsync()  │  • MoveAsync     │  • PreviewAsync()  │ │
│  │  • Clone()         │  • DeleteAsync   │  • ValidateAsync() │ │
│  └────────────────────┴──────────────────┴────────────────────┘ │
│                                                                 │
│  ┌────────────────────┬──────────────────┬────────────────────┐ │
│  │ FileItem           │ OperationResult  │ ValidationResult   │ │
│  │  • FullPath        │  • Success       │  • IsValid         │ │
│  │  • Name            │  • Changes[]     │  • Errors[]        │ │
│  │  • Size            │  • Errors[]      │  • Warnings[]      │ │
│  │  • IsDirectory     │  • Message       │  • ErrorSummary()  │ │
│  └────────────────────┴──────────────────┴────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                                ↑↓
┌─────────────────────────────────────────────────────────────────┐
│                   INFRASTRUCTURE LAYER                          │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ FileOperationBase : IFileOperation (Template Method)    │   │
│  │  • async ValidateAsync() ─┐                             │   │
│  │     ├─> Common validation │                             │   │
│  │     └─> ValidateSpecificAsync() (hook)                  │   │
│  │  • async ExecuteAsync() ──┐                             │   │
│  │     ├─> Validation        │                             │   │
│  │     └─> ExecuteSpecificAsync() (hook)                   │   │
│  │  • async PreviewAsync() ──┐                             │   │
│  │     └─> Sets IsPreviewMode = true                       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                             ↑                                   │
│                             │ (inherits)                        │
│  ┌──────────────┬───────────────────┬───────────────────────┐  │
│  │RenameOperation│ MoveOperation    │ FilterOperation       │  │
│  │ • Prefix      │ • Destination    │ • NamePattern        │  │
│  │ • Suffix      │ • ByExtension    │ • Extensions[]       │  │
│  │ • FindReplace │ • ByDate         │ • SizeRange          │  │
│  │ • Numbering   │ • DateFormat     │ • DateRange          │  │
│  │ • CaseChange  │ • PreserveFolder │ • UseRegex           │  │
│  └──────────────┴───────────────────┴───────────────────────┘  │
│                                                                 │
│  ┌──────────────────┬─────────────────┬──────────────────────┐ │
│  │FileSystemService │ FileValidator   │ OperationContext     │ │
│  │ • GetFilesAsync()│ • ValidatePath()│ • FileSystem         │ │
│  │ • RenameAsync()  │ • ValidateMany()│ • Validator          │ │
│  │ • MoveAsync()    │ • NoCollisions()│ • IsPreviewMode      │ │
│  │ • DeleteAsync()  │ • ValidateChars │ • StopOnError        │ │
│  └──────────────────┴─────────────────┴──────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow Diagram

```
USER ACTION
    │
    ├─> SELECT FILES
    │      │
    │      └─> FileGrid_SelectionChanged
    │             │
    │             └─> ViewModel.SelectedFiles.Add()
    │
    ├─> CONFIGURE OPERATION
    │      │
    │      └─> Click "Rename" Button
    │             │
    │             ├─> RenameConfigDialog.ShowDialog()
    │             │      │
    │             │      └─> User configures (prefix, suffix, etc.)
    │             │
    │             └─> Pipeline.AddOperation(new RenameOperation(config))
    │
    ├─> PREVIEW
    │      │
    │      └─> PreviewPipelineCommand.Execute()
    │             │
    │             ├─> Context.IsPreviewMode = true
    │             │
    │             ├─> Pipeline.ValidateAsync(selectedFiles)
    │             │      │
    │             │      └─> For each operation:
    │             │             └─> Operation.ValidateAsync()
    │             │
    │             ├─> Pipeline.ExecuteAsync(selectedFiles)
    │             │      │
    │             │      └─> For each operation:
    │             │             ├─> Operation.ExecuteAsync()
    │             │             │      │
    │             │             │      ├─> Generate changes
    │             │             │      └─> Don't apply (preview mode)
    │             │             │
    │             │             └─> Update file list for next operation
    │             │
    │             └─> ViewModel.PreviewChanges = all changes
    │                    │
    │                    └─> UI updates via binding
    │
    └─> EXECUTE
           │
           └─> ExecutePipelineCommand.Execute()
                  │
                  ├─> Context.IsPreviewMode = false
                  │
                  ├─> Pipeline.ValidateAsync(selectedFiles)
                  │      │
                  │      └─> Check for errors
                  │
                  ├─> Pipeline.ExecuteAsync(selectedFiles)
                  │      │
                  │      └─> For each operation:
                  │             ├─> Operation.ExecuteAsync()
                  │             │      │
                  │             │      ├─> Generate changes
                  │             │      │
                  │             │      └─> Apply changes:
                  │             │             └─> FileSystem.RenameAsync()
                  │             │             └─> FileSystem.MoveAsync()
                  │             │
                  │             └─> Update file list
                  │
                  ├─> Refresh directory listing
                  │
                  └─> Update UI with results
```

## Operation Lifecycle

```
┌─────────────────────────────────────────────────────────────┐
│                     OPERATION CREATION                      │
│                                                             │
│  User clicks button → Dialog opens → User configures       │
│  → Dialog returns config → Factory creates operation        │
│  → Operation added to pipeline                             │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│                     VALIDATION PHASE                        │
│                                                             │
│  1. Configuration Validation                               │
│     └─> ValidateConfiguration()                            │
│         • Check required fields                            │
│         • Validate patterns (regex)                        │
│         • Check value ranges                               │
│                                                             │
│  2. File Validation                                        │
│     └─> For each file:                                     │
│         • File exists?                                     │
│         • Path valid?                                      │
│         • Permissions OK?                                  │
│                                                             │
│  3. Operation-Specific Validation                          │
│     └─> ValidateSpecificAsync()                            │
│         • Generate preview changes                         │
│         • Check for name collisions                        │
│         • Verify destinations exist                        │
│                                                             │
│  Result: ValidationResult with errors/warnings             │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│                   PREVIEW/EXECUTION PHASE                   │
│                                                             │
│  If Preview Mode:                                          │
│  ├─> Context.IsPreviewMode = true                          │
│  ├─> Generate FileChanges but don't apply                  │
│  └─> Return changes for display                            │
│                                                             │
│  If Execute Mode:                                          │
│  ├─> Context.IsPreviewMode = false                         │
│  ├─> For each file:                                        │
│  │   ├─> Generate change                                   │
│  │   ├─> Apply to file system                             │
│  │   │   └─> FileSystem.RenameAsync()                      │
│  │   │   └─> FileSystem.MoveAsync()                        │
│  │   └─> Mark change as Applied                            │
│  └─> Return OperationResult with changes and errors        │
└─────────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────┐
│                     RESULT PROCESSING                       │
│                                                             │
│  • Collect all changes from all operations                 │
│  • Aggregate statistics (processed, failed)                │
│  • Update UI via data binding                              │
│  • Display status message                                  │
│  • Refresh file listing if executed                        │
└─────────────────────────────────────────────────────────────┘
```

## Extension Points

```
┌─────────────────────────────────────────────────────────────┐
│                   HOW TO ADD NEW OPERATION                  │
└─────────────────────────────────────────────────────────────┘

Step 1: Create Operation Class
┌────────────────────────────────────────────────────────────┐
│ public class MyOperation : FileOperationBase               │
│ {                                                          │
│     public override string OperationId => "my_op";        │
│     public override string DisplayName => "My Op";        │
│                                                            │
│     protected override Task<ValidationResult>             │
│         ValidateSpecificAsync(...) { ... }                │
│                                                            │
│     protected override Task<OperationResult>              │
│         ExecuteSpecificAsync(...) { ... }                 │
│                                                            │
│     public override IFileOperation Clone() { ... }        │
│ }                                                          │
└────────────────────────────────────────────────────────────┘
                         ↓
Step 2: Register in Factory
┌────────────────────────────────────────────────────────────┐
│ factory.RegisterOperation("my_op",                        │
│     ctx => new MyOperation(ctx));                         │
└────────────────────────────────────────────────────────────┘
                         ↓
Step 3: Add UI Button (Optional)
┌────────────────────────────────────────────────────────────┐
│ <Button Content="My Operation"                            │
│         Click="AddMyOperation_Click"/>                    │
│                                                            │
│ private void AddMyOperation_Click(...)                    │
│ {                                                          │
│     var config = ShowConfigDialog();                      │
│     var op = new MyOperation(context, config);            │
│     pipeline.AddOperation(op);                            │
│ }                                                          │
└────────────────────────────────────────────────────────────┘
                         ↓
                   Ready to Use!
```

## Testing Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        UNIT TESTS                           │
│                                                             │
│  Test Individual Operations                                │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ [Test]                                               │  │
│  │ async Task RenameOperation_AddsPrefix()              │  │
│  │ {                                                    │  │
│  │     // Arrange                                       │  │
│  │     var mockFS = new MockFileSystemService();       │  │
│  │     var validator = new FileValidator(mockFS);      │  │
│  │     var context = new OperationContext(mockFS,...); │  │
│  │     var operation = new RenameOperation(context);   │  │
│  │                                                      │  │
│  │     // Act                                           │  │
│  │     var result = await operation.PreviewAsync(...); │  │
│  │                                                      │  │
│  │     // Assert                                        │  │
│  │     Assert.IsTrue(result.Success);                  │  │
│  │     Assert.AreEqual(expected, actual);              │  │
│  │ }                                                    │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                   INTEGRATION TESTS                         │
│                                                             │
│  Test Pipeline Processing                                  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ [Test]                                               │  │
│  │ async Task Pipeline_ExecutesMultipleOps()            │  │
│  │ {                                                    │  │
│  │     var pipeline = new ProcessingPipeline(context); │  │
│  │     pipeline.AddOperation(filterOp);                │  │
│  │     pipeline.AddOperation(renameOp);                │  │
│  │     pipeline.AddOperation(moveOp);                  │  │
│  │                                                      │  │
│  │     var result = await pipeline.ExecuteAsync(...);  │  │
│  │                                                      │  │
│  │     Assert.AreEqual(3, result.StepResults.Count);   │  │
│  │     Assert.IsTrue(result.Success);                  │  │
│  │ }                                                    │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

This comprehensive diagram set shows the complete system architecture, data flow, operation lifecycle, and how all components interact with each other.
