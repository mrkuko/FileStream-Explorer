# FileStream Explorer

A modular WPF desktop application for advanced batch file manipulation with an extensible architecture.

## Features

### Core Capabilities

- **File Explorer Interface** - Browse directories and select multiple files
- **Batch Operations** - Rename, move, and filter files in bulk
- **Multi-Step Processing** - Chain multiple operations in a pipeline
- **Preview Mode** - See changes before applying them
- **Validation** - Comprehensive checks for name collisions and errors
- **Extensible Architecture** - Easy to add new operation types

### Built-in Operations

#### 1. Rename Operation
- Add prefix/suffix to filenames
- Find and replace text
- Sequential numbering with padding
- Case transformations (UPPER, lower, Title Case)
- Normalize spaces
- Preserve or change file extensions

#### 2. Move Operation
- Move files to destination directory
- Organize by file extension
- Organize by date (customizable format)
- Preserve original folder structure
- Automatic subdirectory creation

#### 3. Filter Operation
- Filter by name pattern (with regex support)
- Filter by file extension
- Filter by size range
- Filter by date range
- Include/exclude directories

## Architecture

The application follows a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────┐
│     Presentation Layer (MVVM)           │
│  Views, ViewModels, Commands, Dialogs   │
└─────────────────────────────────────────┘
              ↓↑
┌─────────────────────────────────────────┐
│     Application Layer                   │
│  Pipeline Processor, Operation Executor │
└─────────────────────────────────────────┘
              ↓↑
┌─────────────────────────────────────────┐
│     Domain Layer                        │
│  Interfaces, Models, Base Classes       │
└─────────────────────────────────────────┘
              ↓↑
┌─────────────────────────────────────────┐
│     Infrastructure Layer                │
│  File System, Operations, Validators    │
└─────────────────────────────────────────┘
```

### Design Patterns

- **MVVM Pattern** - Complete separation between UI and business logic
- **Strategy Pattern** - Pluggable operation types via `IFileOperation`
- **Pipeline Pattern** - Sequential processing via `IProcessingPipeline`
- **Command Pattern** - User actions as executable commands
- **Factory Pattern** - Operation creation via `OperationFactory`

## Project Structure

```
FileStream Explorer/
├── Core/
│   ├── Models/              # Domain models
│   │   ├── FileItem.cs
│   │   ├── OperationResult.cs
│   │   └── ValidationResult.cs
│   ├── Interfaces/          # Core interfaces
│   │   ├── IFileOperation.cs
│   │   ├── IFileSystemService.cs
│   │   └── IProcessingPipeline.cs
│   ├── Operations/          # Base operation classes
│   │   ├── FileOperationBase.cs
│   │   └── OperationFactory.cs
│   └── Pipeline/            # Pipeline processor
│       └── ProcessingPipeline.cs
├── Infrastructure/
│   ├── Services/            # File system services
│   │   ├── FileSystemService.cs
│   │   ├── FileValidator.cs
│   │   └── OperationContext.cs
│   └── Operations/          # Concrete operations
│       ├── RenameOperation.cs
│       ├── MoveOperation.cs
│       └── FilterOperation.cs
└── Presentation/
    ├── ViewModels/          # MVVM ViewModels
    │   ├── ViewModelBase.cs
    │   └── MainViewModel.cs
    ├── Commands/            # UI Commands
    │   └── RelayCommand.cs
    ├── Converters/          # Value converters
    │   └── ValueConverters.cs
    └── Dialogs/             # Configuration dialogs
        ├── RenameConfigDialog.xaml
        ├── MoveConfigDialog.xaml
        └── FilterConfigDialog.xaml
```

## Usage Examples

### Example 1: Rename Photos with Sequential Numbers

1. Navigate to your photos folder
2. Select all photo files (Ctrl+Click or Shift+Click)
3. Click "📝 Rename" button
4. Configure:
   - Prefix: `vacation_`
   - Use sequential numbers: ✓
   - Start number: 1
   - Padding: 3
   - Preserve extension: ✓
5. Click "👁️ Preview" to see results
6. Click "▶️ Execute Pipeline" to apply

**Result:** `IMG_1234.jpg` → `vacation_001.jpg`

### Example 2: Organize Documents by Type and Date

1. Select all document files
2. Click "🔍 Filter" to filter by extension
   - Extensions: `.pdf, .docx, .xlsx`
3. Click "📂 Move"
   - Destination: `C:\Organized`
   - Create subdirectories by extension: ✓
   - Create subdirectories by date: ✓
   - Date format: `yyyy-MM`
4. Preview and execute

**Result:** 
- `report.pdf` → `C:\Organized\pdf\2024-12\report.pdf`
- `data.xlsx` → `C:\Organized\xlsx\2024-12\data.xlsx`

### Example 3: Clean Up Filenames

1. Select files with messy names
2. Click "📝 Rename"
   - Find: `_copy`
   - Replace: (empty)
   - Normalize spaces: ✓
   - Case transform: Title Case
3. Preview and execute

**Result:** `my_file_copy  (1).txt` → `My File (1).txt`

### Example 4: Multi-Step Pipeline

Create a complex workflow:

1. **Filter** - Select only .txt files larger than 1KB
2. **Rename** - Add prefix "archive_" and convert to lowercase
3. **Move** - Move to `C:\Archives\text-files`

This processes files through three operations sequentially.

## Validation & Safety

### Pre-Execution Validation

- **File existence** - Verifies all files exist and are accessible
- **Path validity** - Checks for invalid characters and path length
- **Name collision** - Prevents duplicate filenames
- **Permission check** - Validates write access
- **Configuration** - Validates operation settings

### Preview Mode

All operations support preview mode that shows exactly what will happen without making changes:

```csharp
// Preview shows FileChange objects without applying them
var preview = await operation.PreviewAsync(files);
foreach (var change in preview.Changes)
{
    Console.WriteLine($"{change.OriginalPath} → {change.NewPath}");
}
```

### Error Handling

- Individual file errors don't stop the entire operation
- Detailed error messages for each failure
- Operation can be configured to stop on first error
- All errors collected and displayed in results

## Extensibility

### Adding New Operations

See [EXTENSIBILITY.md](EXTENSIBILITY.md) for detailed guide.

Quick example:

```csharp
public class CustomOperation : FileOperationBase
{
    public override string OperationId => "custom";
    public override string DisplayName => "Custom Operation";
    
    protected override async Task<OperationResult> ExecuteSpecificAsync(
        List<FileItem> files, 
        CancellationToken cancellationToken)
    {
        var result = new OperationResult { Success = true };
        
        // Your custom logic here
        foreach (var file in files)
        {
            // Process file
        }
        
        return result;
    }
}
```

Register in factory:

```csharp
_operationFactory.RegisterOperation("custom", ctx => new CustomOperation(ctx));
```

### Extension Points

1. **Custom Operations** - Implement `IFileOperation`
2. **Custom Validators** - Implement `IFileValidator`
3. **Custom File Systems** - Implement `IFileSystemService` (cloud storage, FTP, etc.)
4. **Custom Pipeline Steps** - Extend `ProcessingPipeline`
5. **Custom UI Dialogs** - Create configuration dialogs for operations

## Technical Requirements

- **.NET 8.0** or higher
- **Windows** (WPF application)
- **Visual Studio 2022** or higher (recommended)

## Building the Project

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

Or open `FileStream Explorer.sln` in Visual Studio and press F5.

## Future Enhancements

### Planned Features

- **Undo/Rollback** - Reverse operations
- **Workflow Templates** - Save and load operation pipelines
- **Batch History** - Track all operations performed
- **Parallel Processing** - Process multiple files concurrently
- **Cloud Integration** - Support for cloud storage (OneDrive, Google Drive)
- **Custom Scripting** - Allow custom C# scripts for operations
- **File Content Operations** - Search/replace within file content
- **Duplicate Detection** - Find and manage duplicate files
- **Compression** - Archive files as part of pipeline

### Potential Operation Types

- Content search and replace
- Image resizing/conversion
- File encryption/decryption
- Metadata editing (EXIF, ID3, etc.)
- Hash calculation and verification
- File splitting/merging
- Character encoding conversion

## Architecture Benefits

### SOLID Principles

- **Single Responsibility** - Each class has one clear purpose
- **Open/Closed** - Open for extension (new operations), closed for modification
- **Liskov Substitution** - All operations interchangeable via interface
- **Interface Segregation** - Focused interfaces (IFileOperation, IValidator, etc.)
- **Dependency Inversion** - Depends on abstractions, not concrete classes

### Testability

The architecture enables comprehensive testing:

- **Unit Tests** - Test operations independently
- **Integration Tests** - Test pipeline processing
- **Mock Services** - Replace file system with mocks for testing

Example test:

```csharp
[Test]
public async Task RenameOperation_AddsPrefix_Successfully()
{
    // Arrange
    var mockFileSystem = new MockFileSystemService();
    var validator = new FileValidator(mockFileSystem);
    var context = new OperationContext(mockFileSystem, validator);
    
    var config = new RenameConfiguration { Prefix = "test_" };
    var operation = new RenameOperation(context, config);
    
    var files = new List<FileItem> 
    { 
        new FileItem("C:\\file1.txt") 
    };
    
    // Act
    var result = await operation.PreviewAsync(files);
    
    // Assert
    Assert.IsTrue(result.Success);
    Assert.AreEqual("test_file1.txt", 
        Path.GetFileName(result.Changes[0].NewPath));
}
```

## License

See [LICENSE.txt](LICENSE.txt) for license information.

## Contributing

Contributions welcome! Areas of interest:

1. New operation types
2. Performance improvements
3. UI/UX enhancements
4. Additional validators
5. Documentation improvements

## Support

For issues, questions, or feature requests, please create an issue in the repository.
