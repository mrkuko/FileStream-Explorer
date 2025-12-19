using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FileStreamExplorer.Core.Interfaces;
using FileStreamExplorer.Core.Pipeline;
using FileStreamExplorer.Core.Models;
using FileStreamExplorer.Infrastructure.Operations;
using FileStreamExplorer.Infrastructure.Services;
using FileStreamExplorer.Presentation.ViewModels;

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
        
        // Drag-drop support fields
        private System.Windows.Point _dragStartPoint;
        private bool _isDragging;
        private PipelineOperationItem? _draggedItem;

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
            if (_viewModel.SelectedFile == null)
                return;

            if (_viewModel.SelectedFile.IsDirectory)
            {
                _viewModel.CurrentDirectory = _viewModel.SelectedFile.FullPath;
            }
            else
            {
                try
                {
                    // Open File Explorer and select the file's path
                    var path = _viewModel.SelectedFile.FullPath;
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                catch (Exception ex)
                {
                    _viewModel.StatusMessage = $"Failed to open parent folder: {ex.Message}";
                }
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
                _viewModel.AddPipelineOperation(operation);
            }
        }

        private void AddMoveOperation_Click(object sender, RoutedEventArgs e)
        {
            var configDialog = new MoveConfigDialog();
            if (configDialog.ShowDialog() == true)
            {
                var operation = new MoveOperation(_context, configDialog.Configuration);
                _viewModel.AddPipelineOperation(operation);
            }
        }

        private void AddFilterOperation_Click(object sender, RoutedEventArgs e)
        {
            var configDialog = new FilterConfigDialog();
            if (configDialog.ShowDialog() == true)
            {
                var operation = new FilterOperation(_context, configDialog.Configuration);
                _viewModel.AddPipelineOperation(operation);
            }
        }

        private void ClearPipeline_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearPipelineOperations();
        }

        #region Pipeline Queue Event Handlers

        /// <summary>
        /// Handle double-click on pipeline operation to open configuration dialog
        /// </summary>
        private void PipelineQueueList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PipelineQueueList.SelectedItem is PipelineOperationItem item)
            {
                OpenOperationConfigDialog(item);
            }
        }

        /// <summary>
        /// Opens the appropriate configuration dialog for the operation type
        /// </summary>
        private void OpenOperationConfigDialog(PipelineOperationItem item)
        {
            if (item?.Operation == null) return;

            bool updated = false;

            switch (item.OperationId)
            {
                case "rename":
                    if (item.Operation is RenameOperation renameOp && 
                        renameOp.Configuration is RenameConfiguration renameConfig)
                    {
                        var dialog = new RenameConfigDialog(renameConfig);
                        if (dialog.ShowDialog() == true)
                        {
                            renameOp.Configuration = dialog.Configuration;
                            updated = true;
                        }
                    }
                    break;

                case "move":
                    if (item.Operation is MoveOperation moveOp && 
                        moveOp.Configuration is MoveConfiguration moveConfig)
                    {
                        var dialog = new MoveConfigDialog(moveConfig);
                        if (dialog.ShowDialog() == true)
                        {
                            moveOp.Configuration = dialog.Configuration;
                            updated = true;
                        }
                    }
                    break;

                case "filter":
                    if (item.Operation is FilterOperation filterOp && 
                        filterOp.Configuration is FilterConfiguration filterConfig)
                    {
                        var dialog = new FilterConfigDialog(filterConfig);
                        if (dialog.ShowDialog() == true)
                        {
                            filterOp.Configuration = dialog.Configuration;
                            updated = true;
                        }
                    }
                    break;
            }

            if (updated)
            {
                _viewModel.RefreshPipelineOperations();
                _viewModel.StatusMessage = $"Updated {item.DisplayName} configuration";
            }
        }

        /// <summary>
        /// Handle delete button click for pipeline operation
        /// </summary>
        private void DeletePipelineOperation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is PipelineOperationItem item)
            {
                _viewModel.RemovePipelineOperation(item);
            }
        }

        private void DuplicatePipelineOperation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is PipelineOperationItem item)
            {
                _viewModel.DuplicatePipelineOperation(item);
            }
        }

        #endregion

        #region Drag-Drop Reordering

        /// <summary>
        /// Capture the starting point for drag detection
        /// </summary>
        private void PipelineQueueList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _isDragging = false;
            
            // Find the item under the mouse
            var item = GetItemAtPoint<ListBoxItem>(PipelineQueueList, e.GetPosition(PipelineQueueList));
            if (item != null)
            {
                _draggedItem = item.DataContext as PipelineOperationItem;
            }
        }

        /// <summary>
        /// Initiate drag when mouse moves far enough
        /// </summary>
        private void PipelineQueueList_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedItem == null)
                return;

            System.Windows.Point currentPos = e.GetPosition(null);
            System.Windows.Vector diff = _dragStartPoint - currentPos;

            // Check if we've moved far enough to start dragging
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    
                    // Start the drag-drop operation
                    var data = new System.Windows.DataObject("PipelineOperationItem", _draggedItem);
                    DragDrop.DoDragDrop(PipelineQueueList, data, System.Windows.DragDropEffects.Move);
                    
                    _isDragging = false;
                    _draggedItem = null;
                }
            }
        }

        /// <summary>
        /// Handle drag over to show drop feedback
        /// </summary>
        private void PipelineQueueList_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("PipelineOperationItem"))
            {
                e.Effects = System.Windows.DragDropEffects.None;
                return;
            }

            e.Effects = System.Windows.DragDropEffects.Move;
            e.Handled = true;
        }

        /// <summary>
        /// Handle the drop to reorder items
        /// </summary>
        private void PipelineQueueList_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("PipelineOperationItem"))
                return;

            var droppedItem = e.Data.GetData("PipelineOperationItem") as PipelineOperationItem;
            if (droppedItem == null)
                return;

            // Find the target item
            var targetItem = GetItemAtPoint<ListBoxItem>(PipelineQueueList, e.GetPosition(PipelineQueueList));
            if (targetItem == null)
                return;

            var targetData = targetItem.DataContext as PipelineOperationItem;
            if (targetData == null || targetData == droppedItem)
                return;

            // Get indices (Order is 1-based, so subtract 1 for 0-based index)
            int oldIndex = droppedItem.Order - 1;
            int newIndex = targetData.Order - 1;

            // Perform the move
            _viewModel.MovePipelineOperation(oldIndex, newIndex);

            e.Handled = true;
        }

        /// <summary>
        /// Helper to find item at a specific point
        /// </summary>
        private T? GetItemAtPoint<T>(ItemsControl control, System.Windows.Point point) where T : DependencyObject
        {
            var element = control.InputHitTest(point) as DependencyObject;
            while (element != null && !(element is T) && element != control)
            {
                element = VisualTreeHelper.GetParent(element);
            }
            return element as T;
        }

        #endregion
    }
}