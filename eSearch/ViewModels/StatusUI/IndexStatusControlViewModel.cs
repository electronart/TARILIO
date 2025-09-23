using eSearch.Models.Indexing;
using sun.java2d;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.ViewModels.StatusUI
{
    public class IndexStatusControlViewModel : StatusControlViewModel, IDisposable
    {
        private readonly IIndex _index;
        private readonly Timer _timer;
        private bool _disposed;

        public IndexStatusControlViewModel(IIndex index) : base() { 
            _index = index ?? throw new ArgumentNullException(nameof(index));
            StatusTitle = index.Name; 
            StatusMessage = GetIndexStatusMessage();
            StatusProgress = null; // Hide the progress bar.
            _timer = new Timer(TimeSpan.FromSeconds(30));
            _timer.Elapsed += _timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Start();
        }

        private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            StatusMessage = GetIndexStatusMessage();
        }

        private string GetIndexStatusMessage()
        {
            var lastUpdated = _index.LastUpdated;
            var elapsed = DateTime.UtcNow - lastUpdated;

            string strElapsed  = FormatElapsedTime(elapsed);
            strElapsed = String.Format(S.Get("Updated: {0}"), strElapsed);

            string strSchedule;
            var indexConfig = Program.IndexLibrary.GetConfiguration(_index);
            if (indexConfig.AutomaticUpdates == null)
            {
                strSchedule = S.Get("Not Scheduled.");
            } else
            {
                strSchedule = S.Get("Scheduled");
            }
            return strElapsed + Environment.NewLine + strSchedule;
        }

        private static string FormatElapsedTime(TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds < 30)
            {
                return S.Get("Just now");
            }
            if (elapsed.TotalSeconds < 60)
                return $"{elapsed.Seconds} Seconds ago";
            if (elapsed.TotalMinutes < 60)
                return elapsed.Minutes == 1 ? S.Get("1 Minute ago") : String.Format( S.Get("{0} Minutes ago"), elapsed.Minutes );
            if (elapsed.TotalHours < 24)
                return elapsed.Hours == 1 ? S.Get("1 Hour ago") : String.Format(S.Get("{0} Hours ago"), elapsed.Hours);
            if (elapsed.TotalDays < 30)
                return elapsed.Days == 1 ? S.Get("1 Day ago") : String.Format(S.Get("{0} Days ago"), elapsed.Days);
            // For longer periods, fall back to a date format or custom logic
            return S.Get("Over a Month ago");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _timer?.Stop();
                _timer?.Dispose();
                _disposed = true;
            }
        }
    }
}
