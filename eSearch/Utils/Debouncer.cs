using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace eSearch.Utils
{
    public class Debouncer
    {
        private Timer _debounceTimer;
        private readonly object _lock = new object();
        private readonly TimeSpan delay;
        private Action<object?> callback;
        private object? tag = null;

        public Debouncer(TimeSpan delay, Action<object?> callback, object? tag = null)
        {
            this.delay = delay;
            this.callback = callback;
            this.tag = tag;
        }

        public void OnBurstEvent()
        {
            lock (_lock)
            {
                _debounceTimer?.Stop();
                _debounceTimer?.Dispose();

                _debounceTimer = new Timer(delay);
                _debounceTimer.AutoReset = false;
                _debounceTimer.Elapsed += (sender, e) => { callback(tag); };
                _debounceTimer.Start();
            }
        }
    }
}
