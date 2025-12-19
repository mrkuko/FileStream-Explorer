using System.Windows;
using FileStreamExplorer.Infrastructure.Operations;

namespace FileStream_Explorer
{
    public partial class RenameConfigDialog : Window
    {
        public RenameConfiguration Configuration { get; private set; }

        public RenameConfigDialog()
        {
            InitializeComponent();
            Configuration = new RenameConfiguration();
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

            DialogResult = true;
        }
    }
}
