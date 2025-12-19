using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Pipeline;
using FileStreamExplorer.Infrastructure.Operations;
using FileStreamExplorer.Infrastructure.Services;
using FileStreamExplorer.Presentation.ViewModels;
using FileStreamExplorer.Core.Models;

namespace FileStream_Explorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly IProcessingPipeline _pipeline;
        private readonly IOperationContext _context;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            IFileSystemService fileSystem = new FileSystemService();
            IFileValidator validator = new FileValidator(fileSystem);
            _context = new OperationContext(fileSystem, validator);
            _pipeline = new ProcessingPipeline(_context);

            // Initialize ViewModel
            _viewModel = new MainViewModel(fileSystem, _pipeline, _context);
            DataContext = _viewModel;
        }

        private void FileGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedFile != null && _viewModel.SelectedFile.IsDirectory)
            {
                _viewModel.CurrentDirectory = _viewModel.SelectedFile.FullPath;
            }
        }

        private void FileGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update selected files collection
            _viewModel.SelectedFiles.Clear();
            foreach (FileItem item in FileGrid.SelectedItems)
            {
                _viewModel.SelectedFiles.Add(item);
            }
        }

        private void AddRenameOperation_Click(object sender, RoutedEventArgs e)
        {
            // Show configuration dialog
            var configDialog = new RenameConfigDialog();
            if (configDialog.ShowDialog() == true)
            {
                var operation = new RenameOperation(_context, configDialog.Configuration);
                _pipeline.AddOperation(operation);
                _viewModel.StatusMessage = $"Added rename operation to pipeline ({_pipeline.Operations.Count} operations)";
            }
        }

        private void AddMoveOperation_Click(object sender, RoutedEventArgs e)
        {
            var configDialog = new MoveConfigDialog();
            if (configDialog.ShowDialog() == true)
            {
                var operation = new MoveOperation(_context, configDialog.Configuration);
                _pipeline.AddOperation(operation);
                _viewModel.StatusMessage = $"Added move operation to pipeline ({_pipeline.Operations.Count} operations)";
            }
        }

        private void AddFilterOperation_Click(object sender, RoutedEventArgs e)
        {
            var configDialog = new FilterConfigDialog();
            if (configDialog.ShowDialog() == true)
            {
                var operation = new FilterOperation(_context, configDialog.Configuration);
                _pipeline.AddOperation(operation);
                _viewModel.StatusMessage = $"Added filter operation to pipeline ({_pipeline.Operations.Count} operations)";
            }
        }

        private void ClearPipeline_Click(object sender, RoutedEventArgs e)
        {
            _pipeline.Clear();
            _viewModel.StatusMessage = "Pipeline cleared";
        }
    }
}