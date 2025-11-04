using Avalonia.Controls;
using Avalonia.Threading;
using com.sun.tools.@internal.ws.processor.model.jaxb;
using DocumentFormat.OpenXml.Spreadsheet;
using eSearch.Interop.Indexing;
using eSearch.Models;
using eSearch.Models.Indexing;
using eSearch.Utils;
using eSearch.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.Views
{
    public partial class IndexProgressWindow : Window
    {

        public IndexTask? IndexTask;

        TaskDialogResult DialogResult = TaskDialogResult.OK;

        public IndexProgressWindow()
        {
            InitializeComponent();
            KeyUp += IndexProgressWindow_KeyUp;
            BtnCancel.Click += BtnCancel_Click;
            BtnClose.Click += BtnClose_Click;
            BtnViewLog.Click += BtnViewLog_Click;

            BtnViewLog.IsVisible = false;
            BtnClose.IsVisible = false;
            this.DataContextChanged += IndexProgressWindow_DataContextChanged;
            this.Closed += IndexProgressWindow_Closed;
        }

        private async void IndexProgressWindow_Closed(object? sender, EventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (DataContext is ProgressViewModel pvm)
                {
                    pvm.PropertyChanged -= Pvm_PropertyChanged;
                }
            });
        }

        private void IndexProgressWindow_DataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is ProgressViewModel pvm)
            {
                pvm.PropertyChanged += Pvm_PropertyChanged;
            }
        }

        private async void Pvm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (DataContext is ProgressViewModel pvm)
                {
                    try
                    {
                        TaskbarProgress.SetState(this, TaskbarProgress.TaskbarStates.Normal);
                        TaskbarProgress.SetValue(this, (ulong)pvm.Progress, (ulong)pvm.MaxProgress);
                    }
                    catch (Exception ex)
                    {
                        // Non fatal.
                        Debug.WriteLine(ex.ToString());
                    }
                }
            });
        }

        private void BtnViewLog_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var index = IndexTask?.GetIndex();
            if (index != null)
            {
                string logLocation = Path.Combine(index.GetAbsolutePath(), "IndexTask.txt");
                if (File.Exists(logLocation))
                {
                    eSearch.Models.Utils.CrossPlatformOpenBrowser(new System.Uri(logLocation).AbsoluteUri);
                }
            }
        }

        private void BtnClose_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

        private void BtnCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CancelReq();
        }

        private void IndexProgressWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                CancelReq();
            }
        }

        private async void CancelReq()
        {
            if (DataContext is ProgressViewModel pvm)
            {
                pvm.PropertyChanged -= Pvm_PropertyChanged;
                TaskbarProgress.SetState(this, TaskbarProgress.TaskbarStates.Paused);
                IndexTask?.PauseIndexing();
                var vm = DataContext as ProgressViewModel;
                vm.Status = S.Get("Paused");
                var res = await TaskDialogWindow.OKCancel(S.Get("Cancel Indexing?"), S.Get("Index will only contain partial results."), this);
                if (res == TaskDialogResult.OK)
                {
                    IndexTask?.RequestCancel();
                    DialogResult = TaskDialogResult.Cancel;
                    vm.Status = S.Get("Cancelling");
                }
                IndexTask?.ResumeIndexing();
                pvm.PropertyChanged += Pvm_PropertyChanged;
            }
        }

        // TODO I don't like the way this is implemented - Error handling is complex
        public static async Task<Tuple<TaskDialogResult, IndexTask>> ShowProgressDialogAndStartIndexTask(IndexTask indexTask, Window parent)
        {
            var progressViewModel = indexTask.GetProgressViewModel();
            var indexProgressWindow = new IndexProgressWindow();
            indexProgressWindow.IndexTask = indexTask;
            indexProgressWindow.DataContext = progressViewModel;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += Bw_DoWork;
            bw.RunWorkerCompleted += Bw_RunWorkerCompleted;
            bw.RunWorkerAsync(indexProgressWindow);
            await indexProgressWindow.ShowDialog(parent);
            var dialogResult = indexProgressWindow.DialogResult;
            return new Tuple<TaskDialogResult, IndexTask>(dialogResult, indexTask);
        }

        private static void Bw_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result is IndexProgressWindow progressWindow)
            {
                progressWindow.BtnCancel.IsVisible  = false;
                progressWindow.BtnClose.IsVisible   = true;
                progressWindow.BtnViewLog.IsVisible = true;
            }
        }

        private static void Bw_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (e.Argument is IndexProgressWindow progressWindow)
            {

                try
                {
                    progressWindow.IndexTask?.ResumeIndexing();
                    progressWindow.IndexTask?.Execute(false);
                } catch (Exception ex)
                {

                } finally
                {
                    e.Result = progressWindow;
                }
            }
            
        }
    }
}
