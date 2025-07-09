using Avalonia.Controls;
using eSearch;
using eSearch.Models;
using System;
using System.Threading.Tasks;
#if TARILIO

#endif

namespace DesktopSearch2.Views
{
    public partial class RegistrationWindow : Window
    {

        TaskDialogResult DialogResult = TaskDialogResult.Cancel;

        public RegistrationWindow()
        {
            InitializeComponent();

            textBoxSerial1.TextChanged += TextBoxSerial1_TextChanged;
            textBoxSerial2.TextChanged += TextBoxSerial2_TextChanged;
            textBoxSerial3.TextChanged += TextBoxSerial3_TextChanged;

            buttonOK.Click += ButtonOK_Click;
            buttonCancel.Click += ButtonCancel_Click;

            DialogResult = TaskDialogResult.Cancel;
            KeyUp += RegistrationWindow_KeyUp;
        }

        private void RegistrationWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }

        public static async Task<TaskDialogResult> ShowDialog()
        {
            var dialog = new RegistrationWindow();
            dialog.DataContext = Program.GetMainWindow().DataContext;
            await dialog.ShowDialog(Program.GetMainWindow());
            return dialog.DialogResult;

        }

        private void ButtonCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            DialogResult = TaskDialogResult.Cancel;
            Close();
        }

        private void ButtonOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            bool valid = ValidateSerial();
            if (valid)
            {
                string serial = GetSerial();
                Program.ProgramConfig.Serial = serial;
                Program.SaveProgramConfig();
                DialogResult = TaskDialogResult.OK;
                Close();
            }
        }

        private void TextBoxSerial3_TextChanged(object? sender, TextChangedEventArgs e)
        {
            textBoxSerial3.Text = textBoxSerial3.Text.ToUpper();
        }

        private void TextBoxSerial2_TextChanged(object? sender, TextChangedEventArgs e)
        {
            textBoxSerial2.Text = textBoxSerial2.Text.ToUpper();
            if (textBoxSerial2.Text.Length == 4)
            {
                textBoxSerial3.Focus();
            }
        }

        private void TextBoxSerial1_TextChanged(object? sender, TextChangedEventArgs e)
        {
            textBoxSerial1.Text = textBoxSerial1.Text.ToUpper();

            if (textBoxSerial1.Text.Length == 6)
            {
                textBoxSerial2.Focus();
            }
        }

        private bool ValidateSerial()
        {
#if TARILIO
            string serial = GetSerial();
            if (serial != "")
            {
                var serialType = TARILIO.ProductSerials.isValidSerial(serial, out string year);
                if (serialType == TARILIO.ProductSerials.SerialValidationResult.Valid || serialType == TARILIO.ProductSerials.SerialValidationResult.SearchOnly)
                {
                    labelSerialError.IsVisible = false;
                    return true;
                } else
                {
                    labelSerialError.IsVisible = true;
                    return false;
                }
            } else
            {
                labelSerialError.IsVisible = false;
                return false;
            }
#endif
            return false;
        }

        public string GetSerial()
        {
            try
            {
                if (textBoxSerial1.Text == null) return "a";
                if (textBoxSerial2.Text == null) return "a";
                if (textBoxSerial3.Text == null) return "a";
                if (textBoxSerial1.Text.Trim() == "") return "a";
                if (textBoxSerial2.Text.Trim() == "") return "a";
                if (textBoxSerial3.Text.Trim() == "") return "a";
                return textBoxSerial1.Text.Trim() + "-" + textBoxSerial2.Text.Trim() + "-" + textBoxSerial3.Text.Trim();
            } catch (Exception ex)
            {
                return "";
            }
        }
    }
}
