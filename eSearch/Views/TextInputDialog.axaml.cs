using Avalonia.Controls;
using eSearch.Models;
using eSearch.ViewModels;
using System.Threading.Tasks;
using System;
using Avalonia.Controls.Documents;

namespace eSearch.Views
{
    public partial class TextInputDialog : Window
    {

        public TaskDialogResult DialogResult = TaskDialogResult.Cancel;


        public delegate bool ValidateSubmission(string submission, out string validationError);

        public event ValidateSubmission UserSubmissionValidator = null;

        public TextInputDialog()
        {
            InitializeComponent();
            BtnOK.Click += BtnOK_Click;
            BtnCancel.Click += BtnCancel_Click;
            KeyUp += TextInputDialog_KeyUp;
            Opened += TextInputDialog_Opened;
        }

        private void TextInputDialog_Opened(object? sender, EventArgs e)
        {
            TextBoxInput.Focus();
            TextBoxInput.SelectAll();
        }

        private void TextInputDialog_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }

        private void BtnCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DialogResult = TaskDialogResult.Cancel;
            Close();
        }

        private void BtnOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (UserSubmissionValidator != null)
            {
                if (this.DataContext is TextInputDialogViewModel viewModel)
                {
                    bool isValid = UserSubmissionValidator(viewModel.Text, out string error);
                    if (!isValid)
                    {
                        viewModel.ValidationError = error;
                        return;
                    }
                }
                
            }
            DialogResult = TaskDialogResult.OK;
            Close();
        }

        public static async Task<Tuple<TaskDialogResult, TextInputDialogViewModel>> ShowDialog(Window owner, string title, string label, string watermark, string existingInput, ValidateSubmission Validator = null, int maxLength = 50)
        {
            var inlines = new InlineCollection
            {
                new Run {Text = label}
            };
            return await ShowDialog(owner, title, inlines, watermark, existingInput, Validator, maxLength);
        }

        // Inlines on the main label allow for hyperlinks and custom formatting.
        public static async Task<Tuple<TaskDialogResult, TextInputDialogViewModel>> ShowDialog(Window owner, string title, InlineCollection labelInlines, string watermark, string existingInput, ValidateSubmission Validator = null, int maxLength = 50)
        {
            var dlgConfig = new TextInputDialogViewModel();
            dlgConfig.Title = title;
            dlgConfig.LabelInlines = labelInlines;
            dlgConfig.Watermark = watermark;
            dlgConfig.Text = existingInput;
            dlgConfig.MaxLength = maxLength;
            var dialog = new TextInputDialog();
            dialog.UserSubmissionValidator = Validator;
            dialog.DataContext = dlgConfig;
            await dialog.ShowDialog(owner);
            return new Tuple<TaskDialogResult, TextInputDialogViewModel>(dialog.DialogResult, dlgConfig);
        }

        /*
        public static async Task<Tuple<TaskDialogResult, SearchSettingsViewModel>> ShowDialog()
        {
            var settings = new SearchSettingsViewModel();
            var dialog = new SearchSettingsWindow();
            dialog.DataContext = settings;
            await dialog.ShowDialog(Program.GetMainWindow());
            return new Tuple<TaskDialogResult, SearchSettingsViewModel>(dialog.DialogResult, settings);

        }
        */
    }
}
