# Quick Start Guide

## Prerequisites

- Visual Studio 2022 or later
- .NET 8.0 SDK
- Windows OS (WPF application)

## Building the Application

### Option 1: Using Visual Studio

1. Open `FileStream Explorer.sln` in Visual Studio
2. Press `Ctrl+Shift+B` to build the solution
3. Press `F5` to run with debugging, or `Ctrl+F5` to run without debugging

### Option 2: Using Command Line

```bash
# Navigate to the solution directory
cd "E:\Projects\C#\WPF\FileStream Explorer"

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run the application
dotnet run --project "FileStream Explorer/FileStream Explorer.csproj"
```

## First Time Setup

When you first run the application, it will:

1. Load your Documents folder by default
2. Display an empty file list if the directory is empty
3. Show the operations panel at the bottom

## Basic Usage Tutorial

### Tutorial 1: Rename Files with Prefix

Let's rename some files by adding a prefix.

**Steps:**

1. **Navigate to a folder with files**
   - Use the directory text box to enter a path
   - Or click "⬆ Up" to navigate up
   - Double-click folders in the grid to open them

2. **Select files to rename**
   - Click files to select them
   - Hold `Ctrl` and click to select multiple files
   - Hold `Shift` and click to select a range

3. **Configure rename operation**
   - Click the "📝 Rename" button
   - In the dialog, enter a prefix (e.g., "photo_")
   - Check "Preserve file extension"
   - Click OK

4. **Preview the changes**
   - Click "👁️ Preview" button
   - Review the changes in the preview panel
   - Check that the new names are correct

5. **Execute the operation**
   - If preview looks good, click "▶️ Execute Pipeline"
   - The files will be renamed
   - The file list will refresh automatically

**Result:** All selected files now have the "photo_" prefix

### Tutorial 2: Organize Files by Extension

Let's organize mixed files into folders by extension.

**Steps:**

1. **Select all files** you want to organize
   - Click first file
   - Press `Ctrl+A` to select all, or select manually

2. **Add move operation**
   - Click "📂 Move" button
   - Click "Browse..." to select destination folder
   - Check "Create subdirectories by file extension"
   - Click OK

3. **Preview and execute**
   - Click "👁️ Preview"
   - Verify files will go to correct folders
   - Click "▶️ Execute Pipeline"

**Result:** Files organized like this:
```
Destination/
  ├── pdf/
  │   ├── document1.pdf
  │   └── document2.pdf
  ├── jpg/
  │   ├── photo1.jpg
  │   └── photo2.jpg
  └── txt/
      └── notes.txt
```

### Tutorial 3: Multi-Step Pipeline

Combine multiple operations in sequence.

**Steps:**

1. Select files

2. **Add Filter operation**
   - Click "🔍 Filter"
   - Extensions: `.jpg, .png`
   - Click OK

3. **Add Rename operation**
   - Click "📝 Rename"
   - Prefix: "image_"
   - Use sequential numbers: ✓
   - Click OK

4. **Add Move operation**
   - Click "📂 Move"
   - Destination: Choose folder
   - Create subdirectories by date: ✓
   - Click OK

5. **Preview the entire pipeline**
   - Click "👁️ Preview"
   - See how files will be filtered, renamed, then moved

6. **Execute all operations**
   - Click "▶️ Execute Pipeline"

**Result:** Image files filtered, renamed with sequence numbers, and moved to dated folders

## Tips and Tricks

### Selection Tips

- **Select all files:** `Ctrl+A`
- **Toggle selection:** `Ctrl+Click`
- **Range selection:** `Shift+Click`
- **Clear selection:** Click "❌ Clear Selection" button

### Preview Best Practices

- Always preview before executing destructive operations
- Check the "Preview / Results" panel carefully
- Look for warning messages in the status bar
- Preview shows exact file paths for verification

### Pipeline Management

- **Clear pipeline:** Click "🗑️ Clear Pipeline" to start over
- **Operations execute in order added**
- Each operation processes the output of the previous one
- Pipeline state shown in status bar (e.g., "3 operations")

### Error Handling

- If an operation fails, check the error messages in the preview panel
- Common errors:
  - "File not found" - File was moved or deleted
  - "Access denied" - Insufficient permissions
  - "Name collision" - Target filename already exists
  - "Path too long" - Windows path length limit exceeded

### Performance

- For large file sets (1000+ files), operations may take time
- UI remains responsive during operations
- Use filters to reduce file set size when possible

## Common Workflows

### Workflow 1: Photo Organization

1. Filter: Only .jpg, .png, .heic files
2. Rename: Add "vacation_2024_" prefix with sequential numbers
3. Move: To "Photos/2024-12" with date organization

### Workflow 2: Document Cleanup

1. Filter: Files modified in last 30 days
2. Rename: Normalize spaces, Title Case
3. Move: To "Recent Documents" by extension

### Workflow 3: Archive Old Files

1. Filter: Files older than 1 year
2. Rename: Add "archive_" prefix
3. Move: To "Archives" with year subdirectories

## Troubleshooting

### Application won't start
- Verify .NET 8.0 is installed: `dotnet --version`
- Check for missing dependencies: `dotnet restore`

### Files aren't displaying
- Check folder permissions
- Try navigating to a different folder
- Click "🔄 Refresh" button

### Operation failed
- Check preview panel for specific error messages
- Verify file permissions
- Check for locked files (close in other applications)
- Ensure destination folders exist or can be created

### Preview shows unexpected results
- Review operation configuration
- Check operation order in pipeline
- Clear pipeline and reconfigure

## Keyboard Shortcuts

- `Ctrl+A` - Select all files
- `F5` - Refresh directory
- `Enter` - Open selected folder
- `Backspace` - Navigate up
- `Ctrl+Click` - Toggle selection
- `Shift+Click` - Range selection

## Next Steps

- Read [ARCHITECTURE.md](ARCHITECTURE.md) to understand the design
- Read [EXTENSIBILITY.md](EXTENSIBILITY.md) to add custom operations
- Read [CLASS_STRUCTURES.md](CLASS_STRUCTURES.md) for technical details
- Experiment with different operation combinations
- Try creating your own custom operations

## Getting Help

If you encounter issues:

1. Check error messages in the preview panel
2. Review the status bar for hints
3. Try preview mode to diagnose issues
4. Check file permissions and locks
5. Consult the documentation files

## Safety Reminders

- **Always preview first** before executing operations
- **Backup important files** before bulk operations
- **Test on sample files** before processing valuable data
- **Check preview results** carefully
- **Start with small file sets** to verify behavior

---

Happy file organizing! 🎉
