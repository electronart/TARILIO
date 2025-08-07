using eSearch.Models.Indexing;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch.ViewModels
{
    public class TaskScheduleWindowViewModel : ViewModelBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="schedule">Pass null to leave all fields blank</param>
        /// <returns></returns>
        public static TaskScheduleWindowViewModel FromIndexSchedule(IndexSchedule? schedule)
        {
            var vm = new TaskScheduleWindowViewModel
            {
                StartFrom = schedule?.StartingFrom ?? DateTime.Now,
                RepeatEveryInterval = schedule?.Interval ?? 1,
            };

            string? selectedIntervalSize = vm.AvailableIntervalSizes[0];
            if (schedule?.IntervalSize == IntervalSize.Day)     { selectedIntervalSize = vm.AvailableIntervalSizes[0]; };
            if (schedule?.IntervalSize == IntervalSize.Week)    { selectedIntervalSize = vm.AvailableIntervalSizes[1]; };

            vm.RepeatEveryIntervalSize = selectedIntervalSize;
            return vm;
        }

        public bool TryGetValidSchedule(out IndexSchedule? schedule, out string? errorMsg)
        {
            if (StartFrom == null) { schedule = null; errorMsg = S.Get("Enter starting date."); return false; }
            if (RepeatEveryInterval == null) { schedule = null; errorMsg = S.Get("Select the repeat interval."); return false; };
            if (RepeatEveryIntervalSize == null) { schedule = null; errorMsg = S.Get("Select the repeat interval."); return false; }
            schedule = new IndexSchedule
            {
                StartingFrom = (DateTime)StartFrom,
                Interval     = (int)RepeatEveryInterval
            };

            IntervalSize intervalSize;
            if (RepeatEveryIntervalSize == AvailableIntervalSizes[0]) intervalSize = IntervalSize.Day;
            else intervalSize = IntervalSize.Week;
            schedule.IntervalSize = intervalSize;
            errorMsg = null;
            return true;
        }


        public DateTime? StartFrom
        {
            get
            {
                return _startFrom;
            }
            set
            {
                // Ugh, avalonia... this is not bound to, instead the window itself handles it
                this.RaiseAndSetIfChanged(ref _startFrom, value);
            }
        }

        private DateTime? _startFrom;

        public int? RepeatEveryInterval
        {
            get
            {
                return _repeatEveryInterval;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _repeatEveryInterval, value);
            }
        }

        private int? _repeatEveryInterval = 1;

        public string? RepeatEveryIntervalSize
        {
            get
            {
                return this._repeatEveryIntervalSize;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _repeatEveryIntervalSize, value);
            }
        }

        private string? _repeatEveryIntervalSize = S.Get("Day(s)");

        public List<string> AvailableIntervalSizes { 
            get
            {
                return [ S.Get("Day(s)"), S.Get("Week(s)")];
            } 
        }

        public string DisplayedErrorMsg
        {
            get
            {
                return _displayedErrorMsg;
            }
            set
            {
                this.RaiseAndSetIfChanged(ref _displayedErrorMsg, value);
            }
        }

        private string _displayedErrorMsg = string.Empty;
    }
}
