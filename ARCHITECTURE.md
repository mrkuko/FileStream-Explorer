# FileStream Explorer - Architecture Overview

## High-Level Architecture

The application follows a layered architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  MainWindow  │  │  ViewModels  │  │   Converters │  │
│  │    (XAML)    │  │   (MVVM)     │  │              │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────┘
                          ↓↑
┌─────────────────────────────────────────────────────────┐
│                   Application Layer                      │
│  ┌──────────────────────┐  ┌────────────────────────┐  │
│  │  Pipeline Processor  │  │  Operation Executor    │  │
│  │  (Chain of Resp.)    │  │  (Command Pattern)     │  │
│  └──────────────────────┘  └────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                          ↓↑
┌─────────────────────────────────────────────────────────┐
│                      Domain Layer                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  IOperation  │  │  FileItem    │  │  Validators  │  │
│  │ (Strategy)   │  │  (Models)    │  │              │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────┘
                          ↓↑
┌─────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                    │
│  ┌──────────────────┐  ┌──────────────────────────┐    │
│  │  FileSystem      │  │  Operation               │    │
│  │  Service         │  │  Implementations         │    │
│  └──────────────────┘  └──────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Domain Layer (Core)
- **Models**: FileItem, FileOperation, OperationResult
- **Interfaces**: IFileOperation, IFileValidator, IOperationContext
- **Value Objects**: FilePath, OperationStatus, ValidationResult

### 2. Infrastructure Layer
- **FileSystemService**: Handles all file I/O operations
- **Operation Implementations**: Concrete operation classes
- **Validators**: File system validation logic

### 3. Application Layer
- **PipelineProcessor**: Executes multiple operations in sequence
- **OperationExecutor**: Coordinates operation execution
- **PreviewService**: Generates operation previews

### 4. Presentation Layer
- **ViewModels**: MainViewModel, OperationViewModel
- **Views**: MainWindow (file explorer UI)
- **Converters**: Value converters for UI binding

## Design Patterns

### Strategy Pattern (Operations)
Each operation type implements IFileOperation interface, enabling easy addition of new operations.

### Pipeline Pattern (Multi-Step Processing)
Operations can be chained together in a processing pipeline where each step's output becomes the next step's input.

### MVVM Pattern
Complete separation between UI (View), presentation logic (ViewModel), and business logic (Model).

### Command Pattern
User actions are encapsulated as commands that can be executed, previewed, and potentially undone.

## Extensibility Points

### Adding New Operations
1. Create class implementing IFileOperation
2. Register in OperationFactory
3. Add UI configuration if needed

### Adding New Validators
1. Implement IFileValidator
2. Register in ValidationService
3. Use in operation pipeline

### Adding New Processing Steps
1. Inherit from ProcessingStepBase
2. Implement Execute and Preview methods
3. Add to pipeline configuration

## Key Features

- **Async Operations**: All file operations use async/await to prevent UI freezing
- **Preview Mode**: Dry-run capability for all operations
- **Validation**: Pre-execution validation with detailed error reporting
- **Error Handling**: Comprehensive error handling with rollback capability
- **Observable Collections**: Real-time UI updates through data binding
- **Serializable Workflows**: Save/load operation configurations
