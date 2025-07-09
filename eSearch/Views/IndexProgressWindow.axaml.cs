using Avalonia.Controls;
using com.sun.tools.@internal.ws.processor.model.jaxb;
using DocumentFormat.OpenXml.Spreadsheet;
using eSearch.Models;
using eSearch.Models.Indexing;
using eSearch.ViewModels;
using System;
using System.ComponentModel;
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
        }

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
                progressWindow.IndexTask?.ResumeIndexing();
                progressWindow.IndexTask?.Execute();
                e.Result = progressWindow;
            }
            
        }
    }
}
