using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using FileStreamExplorer.Infrastructure.Operations;

namespace FileStream_Explorer
{
    public partial class FilterConfigDialog : Window
    {
        public FilterConfiguration Configuration { get; private set; }

        public FilterConfigDialog() : this(null)
        {
        }

        public FilterConfigDialog(FilterConfiguration? existingConfig)
        {
            InitializeComponent();
            Configuration = existingConfig ?? new FilterConfiguration();
            LoadConfigurationToUI();
        }

        private void LoadConfigurationToUI()
        {
            NamePatternTextBox.Text = Configuration.NamePattern;
            UseRegexCheckBox.IsChecked = Configuration.UseRegex;
            IncludeDirectoriesCheckBox.IsChecked = Configuration.IncludeDirectories;
            ExtensionsTextBox.Text = string.Join(", ", Configuration.Extensions ?? new List<string>());
            MinSizeTextBox.Text = Configuration.MinSize?.ToString() ?? string.Empty;
            MaxSizeTextBox.Text = Configuration.MaxSize?.ToString() ?? string.Empty;
            MinDatePicker.SelectedDate = Configuration.MinDate;
            MaxDatePicker.SelectedDate = Configuration.MaxDate;
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
