# Extensibility Guide - Adding New Operations

This document demonstrates how to add new file operation types to FileStream Explorer.

## Example: Adding a "Content Search" Operation

### Step 1: Create the Operation Class

```csharp
using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Models;
using FileStreamExplorer.Core.Operations;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStreamExplorer.Infrastructure.Operations
{
    /// <summary>
    /// Configuration for content search operation
    /// </summary>
    public class ContentSearchConfiguration
    {
        public string SearchText { get; set; } = string.Empty;
        public bool CaseSensitive { get; set; }
        public bool UseRegex { get; set; }
    }

    /// <summary>
    /// Searches file content and filters files containing search text
    /// </summary>
    public class ContentSearchOperation : FileOperationBase, IConfigurableOperation
    {
        public override string OperationId => "content_search";
        public override string DisplayName => "Search Content";
        public override string Description => "Filter files by content";

        private ContentSearchConfiguration _config;

        public object Configuration
        {
            get => _config;
            set => _config = value as ContentSearchConfiguration ?? new ContentSearchConfiguration();
        }

        public ContentSearchOperation(IOperationContext context) : base(context)
        {
            _config = new ContentSearchConfiguration();
        }

        public ContentSearchOperation(IOperationContext context, ContentSearchConfiguration config) 
            : base(context)
        {
            _config = config ?? new ContentSearchConfiguration();
        }

        public ValidationResult ValidateConfiguration()
        {
            var result = new ValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(_config.SearchText))
            {
                result.AddError("Search text is required");
            }

            if (_config.UseRegex)
            {
                try
                {
                    _ = new System.Text.RegularExpressions.Regex(_config.SearchText);
                }
                catch
                {
                    result.AddError($"Invalid regex pattern: {_config.SearchText}");
                }
            }

            return result;
        }

        protected override async Task<ValidationResult> ValidateSpecificAsync(
            List<FileItem> files, 
            CancellationToken cancellationToken)
        {
            return await Task.FromResult(ValidateConfiguration());
        }

        protected override async Task<OperationResult> ExecuteSpecificAsync(
            List<FileItem> files, 
            CancellationToken cancellationToken)
        {
            var result = new OperationResult { Success = true };

            foreach (var file in files.Where(f => !f.IsDirectory))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    bool matches = await CheckFileContentAsync(file.FullPath);

                    if (matches)
                    {
                        result.AddChange(new FileChange(file.FullPath, file.FullPath, ChangeType.Modify)
                        {
                            Description = "Contains search text",
                            Applied = true
                        });
                    }
                }
                catch (System.Exception ex)
                {
                    result.AddError($"Error searching {file.FullPath}: {ex.Message}");
                }
            }

            return result;
        }

        private async Task<bool> CheckFileContentAsync(string filePath)
        {
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                
                if (_config.UseRegex)
                {
                    var regex = new System.Text.RegularExpressions.Regex(
                        _config.SearchText,
                        _config.CaseSensitive 
                            ? System.Text.RegularExpressions.RegexOptions.None 
                            : System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    return regex.IsMatch(content);
                }
                else
                {
                    var comparison = _config.CaseSensitive 
                        ? System.StringComparison.Ordinal 
                        : System.StringComparison.OrdinalIgnoreCase;
                    return content.Contains(_config.SearchText, comparison);
                }
            }
            catch
            {
                return false;
            }
        }

        public override IFileOperation Clone()
        {
            return new ContentSearchOperation(Context, new ContentSearchConfiguration
            {
                SearchText = _config.SearchText,
                CaseSensitive = _config.CaseSensitive,
                UseRegex = _config.UseRegex
            });
        }
    }
}
```

### Step 2: Register the Operation

In `OperationFactory.RegisterDefaultOperations()`:

```csharp
private void RegisterDefaultOperations()
{
    // Existing operations
    RegisterOperation("rename", ctx => new RenameOperation(ctx));
    RegisterOperation("move", ctx => new MoveOperation(ctx));
    RegisterOperation("filter", ctx => new FilterOperation(ctx));
    
    // New operation
    RegisterOperation("content_search", ctx => new ContentSearchOperation(ctx));
}
```

### Step 3: Create Configuration Dialog (Optional)

Create XAML and code-behind for `ContentSearchConfigDialog` similar to other dialogs.

### Step 4: Add UI Button

In `MainWindow.xaml`, add button to operations panel:

```xml
<Button Content="🔎 Search Content" 
        ToolTip="Add content search operation to pipeline"
        Click="AddContentSearchOperation_Click"/>
```

In `MainWindow.xaml.cs`:

```csharp
private void AddContentSearchOperation_Click(object sender, RoutedEventArgs e)
{
    var configDialog = new ContentSearchConfigDialog();
    if (configDialog.ShowDialog() == true)
    {
        var operation = new ContentSearchOperation(_context, configDialog.Configuration);
        _pipeline.AddOperation(operation);
        _viewModel.StatusMessage = $"Added content search to pipeline ({_pipeline.Operations.Count} operations)";
    }
}
```

## Key Extension Points

### 1. Custom Operations
Inherit from `FileOperationBase` and implement:
- `ValidateSpecificAsync()` - Validation logic
- `ExecuteSpecificAsync()` - Core operation logic
- `Clone()` - Deep copy for pipeline serialization

### 2. Custom Validators
Implement `IFileValidator` for specialized validation rules.

### 3. Custom File System Operations
Extend `IFileSystemService` for platform-specific or cloud storage operations.

### 4. Pipeline Steps
Create complex workflows by chaining operations in `IProcessingPipeline`.

## Preview & Validation Features

### Preview Mode
All operations support preview mode through the `IOperationContext.IsPreviewMode` flag:

```csharp
public async Task<OperationResult> PreviewAsync(IEnumerable<FileItem> files)
{
    Context.IsPreviewMode = true;
    var result = await ExecuteAsync(files);
    Context.IsPreviewMode = false;
    return result;
}
```

### Validation Layers

1. **Configuration Validation** - Validates operation settings before execution
2. **File Validation** - Checks file existence, permissions, path validity
3. **Collision Detection** - Prevents name conflicts before execution
4. **Path Validation** - Ensures valid characters and path length

### Example: Using Validation

```csharp
// Validate before execution
var validation = await operation.ValidateAsync(files);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"[{error.Type}] {error.Message}");
    }
    return;
}

// Preview changes
var preview = await operation.PreviewAsync(files);
Console.WriteLine($"Will process {preview.ProcessedCount} files");
foreach (var change in preview.Changes)
{
    Console.WriteLine(change.ToString());
}

// Execute
var result = await operation.ExecuteAsync(files);
```

## Advanced Features

### Pipeline Serialization

Operations can be serialized to save workflows:

```csharp
// Save pipeline configuration
var config = new PipelineConfiguration
{
    Operations = _pipeline.Operations
        .Select(op => new OperationConfig
        {
            Id = op.OperationId,
            Configuration = (op as IConfigurableOperation)?.Configuration
        })
        .ToList()
};

var json = JsonSerializer.Serialize(config);
File.WriteAllText("workflow.json", json);

// Load pipeline configuration
var json = File.ReadAllText("workflow.json");
var config = JsonSerializer.Deserialize<PipelineConfiguration>(json);

foreach (var opConfig in config.Operations)
{
    var operation = _factory.CreateOperation(opConfig.Id);
    if (operation is IConfigurableOperation configurable)
    {
        configurable.Configuration = opConfig.Configuration;
    }
    _pipeline.AddOperation(operation);
}
```

### Error Handling & Rollback

The architecture supports operation rollback (not yet implemented):

```csharp
public interface IReversibleOperation : IFileOperation
{
    Task<OperationResult> UndoAsync(OperationResult originalResult);
}
```

### Logging Integration

Add logging to track operation history:

```csharp
public interface IOperationLogger
{
    void LogOperation(string operationId, OperationResult result);
    IEnumerable<OperationLog> GetHistory();
}
```

## Best Practices

1. **Always validate** before execution
2. **Use preview mode** for destructive operations
3. **Handle cancellation** via CancellationToken
4. **Report progress** for long-running operations
5. **Preserve file metadata** when possible
6. **Test with edge cases** (locked files, long paths, special characters)
7. **Document configuration** options clearly
