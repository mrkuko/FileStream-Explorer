using System.Windows;
using FileStreamExplorer.Infrastructure.Operations;

namespace FileStream_Explorer
{
    public partial class RenameConfigDialog : Window
    {
        public RenameConfiguration Configuration { get; private set; }

        public RenameConfigDialog() : this(null)
        {
        }

        public RenameConfigDialog(RenameConfiguration? existingConfig)
        {
            InitializeComponent();
            Configuration = existingConfig ?? new RenameConfiguration();
            LoadConfigurationToUI();
        }

        private void LoadConfigurationToUI()
        {
            PrefixTextBox.Text = Configuration.Prefix;
            SuffixTextBox.Text = Configuration.Suffix;
            PreserveExtensionCheckBox.IsChecked = Configuration.PreserveExtension;
            FindTextBox.Text = Configuration.FindText;
            ReplaceTextBox.Text = Configuration.ReplaceText;
            UseNumberingCheckBox.IsChecked = Configuration.UseSequentialNumbering;
            NormalizeSpacesCheckBox.IsChecked = Configuration.NormalizeSpaces;
            KeepCoreNameCheckBox.IsChecked = Configuration.KeepCoreName;
            StartNumberTextBox.Text = Configuration.StartNumber.ToString();
            NumberPaddingTextBox.Text = Configuration.NumberPadding.ToString();
            CaseTransformComboBox.SelectedIndex = Configuration.CaseTransform switch
            {
                CaseTransform.Uppercase => 1,
                CaseTransform.Lowercase => 2,
                CaseTransform.TitleCase => 3,
                _ => 0
            };
            UseRegexCheckBox.IsChecked = Configuration.UseRegex;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Configuration.Prefix = PrefixTextBox.Text;
            Configuration.Suffix = SuffixTextBox.Text;
            Configuration.PreserveExtension = PreserveExtensionCheckBox.IsChecked ?? true;
            Configuration.FindText = FindTextBox.Text;
            Configuration.ReplaceText = ReplaceTextBox.Text;
            Configuration.UseSequentialNumbering = UseNumberingCheckBox.IsChecked ?? false;
            Configuration.NormalizeSpaces = NormalizeSpacesCheckBox.IsChecked ?? false;
            Configuration.KeepCoreName = KeepCoreNameCheckBox.IsChecked ?? true;

            if (int.TryParse(StartNumberTextBox.Text, out int startNum))
                Configuration.StartNumber = startNum;

            if (int.TryParse(NumberPaddingTextBox.Text, out int padding))
                Configuration.NumberPadding = padding;

            Configuration.CaseTransform = CaseTransformComboBox.SelectedIndex switch
            {
                1 => CaseTransform.Uppercase,
                2 => CaseTransform.Lowercase,
                3 => CaseTransform.TitleCase,
                _ => CaseTransform.None
            };

            Configuration.UseRegex = UseRegexCheckBox.IsChecked ?? false;

            DialogResult = true;
        }
    }
}
