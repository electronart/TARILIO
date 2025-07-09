using Avalonia.Controls;
using eSearch.Models;
using eSearch.ViewModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{
    public partial class TaskDialogWindow : Window
    {
        private string _result = string.Empty;


        public TaskDialogWindow()
        {
            InitializeComponent();
            BindEvents();
            KeyUp += TaskDialogWindow_KeyUp;
        }

        private void TaskDialogWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }

        public static async Task<TaskDialogResult> RetryCancel(Exception ex, Window owner)
        {
            Debug.WriteLine("Exception was thrown:");
            Debug.WriteLine(ex.ToString());
            var res = await RetryCancel(
                S.Get("Something went wrong. Retry?"), 
                ex.Message,
                owner
            );
            return res;
        }

        public static async Task OKDialog(string MainInstruction, string content, Window owner, string customOKString = null)
        {
            if (customOKString == null)
            {
                customOKString = S.Get("OK");
            }
            var _dlgOptions = new TaskDialogWindowViewModel(
                MainInstruction,
                content,
                customOKString, string.Empty, string.Empty
            );
            var okDialog = new TaskDialogWindow();
            okDialog.DataContext = _dlgOptions;
            var r = await okDialog.ShowDialog<object>(owner); // Assignment not really necessary, but useful for UI flow.
        }

        public static async Task<TaskDialogResult> OKCancel(string MainInstruction, string content, Window owner)
        {
            string txtOK = S.Get("OK");
            return await OKCancel(MainInstruction, content, owner, txtOK);
        }

        public static async Task<TaskDialogResult> OKCancel(string MainInstruction, string content, Window owner, string customOKString)
        {
            string txtCancel = S.Get("Cancel");
            var _dlgOptions = new TaskDialogWindowViewModel(
               MainInstruction,       // MainInstruction
               content,   // Content
               customOKString, txtCancel, string.Empty          // Dialog Buttons
               );
            var okCancelDialog = new TaskDialogWindow();
            okCancelDialog.DataContext = _dlgOptions;
            var r = await okCancelDialog.ShowDialog<object>(owner);
            var res = okCancelDialog.GetDialogResult();
            if (res == customOKString)
            {
                return TaskDialogResult.OK;
            }
            return TaskDialogResult.Cancel;
        }

        public static async Task<TaskDialogResult> RetryCancel(string MainInstruction, string content, Window owner)
        {
            string txtRetry     = S.Get("Retry");
            string txtCancel    = S.Get("Cancel");
            var _dlgOptions = new TaskDialogWindowViewModel(
               MainInstruction,       // MainInstruction
               content,   // Content
               txtRetry, txtCancel, string.Empty          // Dialog Buttons
               );
            var retryCancelDialog = new TaskDialogWindow();
            retryCancelDialog.DataContext = _dlgOptions;
            var r = await retryCancelDialog.ShowDialog<object>(owner);
            var res = retryCancelDialog.GetDialogResult();
            if (res == txtRetry)
            {
                return TaskDialogResult.Retry;
            }
            return TaskDialogResult.Cancel;
        }

        /// <summary>
        /// Will return OK on delete.
        /// </summary>
        /// <param name="MainInstruction"></param>
        /// <param name="content"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static async Task<TaskDialogResult> DeleteCancel(string MainInstruction, string content, Window owner)
        {
            string txtDelete = S.Get("Delete");
            string txtCancel = S.Get("Cancel");
            var _dlgOptions = new TaskDialogWindowViewModel(
               MainInstruction,       // MainInstruction
               content,   // Content
               txtDelete, txtCancel, string.Empty          // Dialog Buttons
               );
            var retryCancelDialog = new TaskDialogWindow();
            retryCancelDialog.DataContext = _dlgOptions;
            var r = await retryCancelDialog.ShowDialog<object>(owner);
            var res = retryCancelDialog.GetDialogResult();
            if (res == txtDelete)
            {
                return TaskDialogResult.OK;
            }
            return TaskDialogResult.Cancel;
        }

        public string GetDialogResult()
        {
            return _result;
        }

        private void BindEvents()
        {
            Button1.Click += ButtonClick;
            Button2.Click += ButtonClick;
            Button3.Click += ButtonClick;
        }

        private void ButtonClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Button clickedButton = (Button)sender;
            _result = (string)clickedButton.Content;
            Debug.WriteLine("Result of dialog: " + _result);
            this.Close();
        }

        
    }
}
