using Avalonia.Controls;
using eSearch.ViewModels;
using System;
using System.Globalization;

namespace eSearch.Views
{
    public partial class TaskScheduleWindow : Window
    {
        public TaskScheduleWindow()
        {
            InitializeComponent();
            KeyUp += TaskScheduleWindow_KeyUp;

            ButtonCancel.Click += ButtonCancel_Click;
            ButtonOK.Click += ButtonOK_Click;

            DataContextChanged += TaskScheduleWindow_DataContextChanged;
        }

        private void TaskScheduleWindow_DataContextChanged(object? sender, System.EventArgs e)
        {
            if (DataContext is TaskScheduleWindowViewModel vm)
            {
                var date = vm.StartFrom;
                if ( date == null)
                {
                    date = DateTime.Now;   
                }
                // TODO This is ugly...
                StartDatePicker.SelectedDate = date;

                string hours    = ((DateTime)date).ToString("HH", CultureInfo.InvariantCulture);
                string minutes  = ((DateTime)date).ToString("mm", CultureInfo.InvariantCulture);

                StartTimePicker.SelectedTime = new System.TimeSpan(
                    int.Parse(hours), 
                    int.Parse(minutes), 
                    1);

                StartDatePicker.SelectedDateChanged += StartDatePicker_SelectedDateChanged;
                StartTimePicker.SelectedTimeChanged += StartTimePicker_SelectedTimeChanged;
            }
        }

        private void StartDatePicker_SelectedDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
        {
            DateControlsValueChanged();
        }

        private void StartTimePicker_SelectedTimeChanged(object? sender, TimePickerSelectedValueChangedEventArgs e)
        {
            DateControlsValueChanged();
        }

        private void DateControlsValueChanged()
        {
            if (StartDatePicker.SelectedDate == null) return;
            if (StartTimePicker.SelectedTime == null) return;
            DateTime dt = ConvertFromDateTimeOffset(StartDatePicker.SelectedDate ?? new DateTimeOffset());
            dt.Add(StartTimePicker.SelectedTime ?? new TimeSpan());
            if (DataContext is TaskScheduleWindowViewModel vm)
            {
                vm.StartFrom = dt; // cursed code..
            }
        }

        //UGH
        static DateTime ConvertFromDateTimeOffset(DateTimeOffset dateTime)
        {
            if (dateTime.Offset.Equals(TimeSpan.Zero))
                return dateTime.UtcDateTime;
            else if (dateTime.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.DateTime)))
                return DateTime.SpecifyKind(dateTime.DateTime, DateTimeKind.Local);
            else
                return dateTime.DateTime;
        }

        private void ButtonOK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is TaskScheduleWindowViewModel vm)
            {
                if (vm.TryGetValidSchedule(out var schedule, out var error))
                {
                    Close(schedule);
                } else
                {
                    vm.DisplayedErrorMsg = error ?? string.Empty; // Should never actually be null. just for compiler.
                }
            }
        }

        private void ButtonCancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

        private void TaskScheduleWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Escape)
            {
                Close();
            }
        }
    }
}
