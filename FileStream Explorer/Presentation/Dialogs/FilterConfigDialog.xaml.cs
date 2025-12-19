using System;
using System.Linq;
using System.Windows;
using FileStreamExplorer.Infrastructure.Operations;

namespace FileStream_Explorer
{
    public partial class FilterConfigDialog : Window
    {
        public FilterConfiguration Configuration { get; private set; }

        public FilterConfigDialog()
        {
            InitializeComponent();
            Configuration = new FilterConfiguration();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Configuration.NamePattern = NamePatternTextBox.Text;
            Configuration.UseRegex = UseRegexCheckBox.IsChecked ?? false;
            Configuration.IncludeDirectories = IncludeDirectoriesCheckBox.IsChecked ?? true;

            // Parse extensions
            if (!string.IsNullOrWhiteSpace(ExtensionsTextBox.Text))
            {
                Configuration.Extensions = ExtensionsTextBox.Text
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .ToList();
            }

            // Parse sizes
            if (long.TryParse(MinSizeTextBox.Text, out long minSize))
                Configuration.MinSize = minSize;

            if (long.TryParse(MaxSizeTextBox.Text, out long maxSize))
                Configuration.MaxSize = maxSize;

            // Parse dates
            Configuration.MinDate = MinDatePicker.SelectedDate;
            Configuration.MaxDate = MaxDatePicker.SelectedDate;

            DialogResult = true;
        }
    }
}
