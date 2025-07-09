using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using eSearch.Models.TaskManagement;
using eSearch.Views;
using ReactiveUI;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.ViewModels
{
    public class ProgressViewModel : ViewModelBase
    {

        IProgressQueryableTask? WatchTask;
        Timer? timer = null;


        public void BeginWatching(IProgressQueryableTask watchTask)
        {
            WatchTask = watchTask;
            if (timer != null) timer.Stop();
            timer = new Timer();
            timer.Interval = 250;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            Progress = WatchTask?.GetProgress()         ?? 0;
            MaxProgress = WatchTask?.GetMaxProgress()   ?? 1;
            Status = WatchTask?.GetStatusString()       ?? "...";
        }

        public void EndWatching()
        {
            timer?.Stop();
            timer = null;

            Progress = WatchTask?.GetProgress() ?? 0;
            MaxProgress = WatchTask?.GetMaxProgress() ?? 1;
            Status = WatchTask?.GetStatusString() ?? "...";
        }

        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    this.RaisePropertyChanged(nameof(Progress));
                }
            }
        }

        public int _maxProgress = 1;

        public int MaxProgress
        {
            get
            {
                return _maxProgress;
            } set
            {
                this.RaiseAndSetIfChanged(ref _maxProgress, value);
            }
        }

        public bool IsFinished
        {
            get
            {
                return _isFinished;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _isFinished, value);
            }
        }

        private bool _isFinished = false;

        private string _status = "Unknown";
        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    this.RaisePropertyChanged(nameof(Status));
                }
            }
        }

        //public event PropertyChangedEventHandler PropertyChanged;
    }
}
