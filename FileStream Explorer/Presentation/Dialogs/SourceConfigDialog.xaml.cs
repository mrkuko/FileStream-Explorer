using System.Windows;
using FileStreamExplorer.Infrastructure.Operations;

namespace FileStream_Explorer
{
    public partial class SourceConfigDialog : Window
    {
        public SourceConfiguration Configuration { get; private set; }

        public SourceConfigDialog() : this(null)
        {
        }

        public SourceConfigDialog(SourceConfiguration? existingConfig)
        {
            InitializeComponent();
            Configuration = existingConfig ?? new SourceConfiguration();
            LoadConfigurationToUI();
        }

        private void LoadConfigurationToUI()
        {
            SourceDirectoryTextBox.Text = Configuration.SourceDirectory;
            FilePatternTextBox.Text = string.IsNullOrEmpty(Configuration.FilePattern) ? "*.*" : Configuration.FilePattern;
            IncludeSubdirectoriesCheckBox.IsChecked = Configuration.IncludeSubdirectories;
            MergeWithExistingCheckBox.IsChecked = Configuration.MergeWithExisting;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select source directory";
            
            if (!string.IsNullOrEmpty(SourceDirectoryTextBox.Text))
            {
                dialog.SelectedPath = SourceDirectoryTextBox.Text;
            }
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SourceDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SourceDirectoryTextBox.Text))
            {
                System.Windows.MessageBox.Show("Please specify a source directory.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Configuration.SourceDirectory = SourceDirectoryTextBox.Text;
            Configuration.FilePattern = FilePatternTextBox.Text;
            Configuration.IncludeSubdirectories = IncludeSubdirectoriesCheckBox.IsChecked ?? false;
            Configuration.MergeWithExisting = MergeWithExistingCheckBox.IsChecked ?? true;

            DialogResult = true;
        }
    }
}
