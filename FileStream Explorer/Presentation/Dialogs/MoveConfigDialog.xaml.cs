using System.Windows;
using FileStreamExplorer.Infrastructure.Operations;
using Microsoft.Win32;

namespace FileStream_Explorer
{
    public partial class MoveConfigDialog : Window
    {
        public MoveConfiguration Configuration { get; private set; }

        public MoveConfigDialog() : this(null)
        {
        }

        public MoveConfigDialog(MoveConfiguration? existingConfig)
        {
            InitializeComponent();
            Configuration = existingConfig ?? new MoveConfiguration();
            LoadConfigurationToUI();
        }

        private void LoadConfigurationToUI()
        {
            DestinationTextBox.Text = Configuration.DestinationDirectory;
            ByExtensionCheckBox.IsChecked = Configuration.CreateSubdirectoriesByExtension;
            ByDateCheckBox.IsChecked = Configuration.CreateSubdirectoriesByDate;
            DateFormatTextBox.Text = Configuration.DateFormat;
            PreserveFolderCheckBox.IsChecked = Configuration.PreserveFolderStructure;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select destination directory";
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DestinationTextBox.Text = dialog.SelectedPath;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DestinationTextBox.Text))
            {
                System.Windows.MessageBox.Show("Please specify a destination directory.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Configuration.DestinationDirectory = DestinationTextBox.Text;
            Configuration.CreateSubdirectoriesByExtension = ByExtensionCheckBox.IsChecked ?? false;
            Configuration.CreateSubdirectoriesByDate = ByDateCheckBox.IsChecked ?? false;
            Configuration.DateFormat = DateFormatTextBox.Text;
            Configuration.PreserveFolderStructure = PreserveFolderCheckBox.IsChecked ?? false;

            DialogResult = true;
        }
    }
}
