using DynamicData;
using eSearch.Models.Indexing;
using Microsoft.Win32.TaskScheduler;
using SharpCompress;
using System;
using System.Reflection;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace eSearch
{
    public static class ScheduleUtils
    {

        /// <summary>
        /// Create/Update Index Task Schedule.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="schedule">Pass null to delete existing</param>
        public static void CreateUpdateScheduleCrossPlatform(IIndex index, IndexSchedule? schedule)
        {

            using (TaskService ts = new TaskService())
            {
                #region 0. Ensure the eSearch Task Scheduler Folder exists
                ts.RootFolder.CreateFolder("eSearch", null, false);
                #endregion
                #region 1. Delete existing task for this index if it already exists.
                ts.GetFolder("\\eSearch").Tasks.ForEach(task =>
                {
                    if (task.Definition.Data == "eSearch_IndexID=" + index.Id)
                    {
                        ts.GetFolder("\\eSearch").DeleteTask(task.Name, false);
                    }
                });
                #endregion
                #region 2. Register New Task if schedule is not null (Otherwise this is a delete op)
                if (schedule != null)
                {
                    var newTaskDefinition = CreateTaskDefinition(index, schedule);
                    ts.RootFolder.RegisterTaskDefinition($"eSearch\\Scheduled Index Update for {index.Name}", newTaskDefinition);
                }
                #endregion
            }
        }

        private static TaskDefinition CreateTaskDefinition(IIndex index, IndexSchedule schedule)
        {
            TaskDefinition td = TaskService.Instance.NewTask();
            td.Data = "eSearch_IndexID=" + index.Id;
            td.RegistrationInfo.Description = string.Format( S.Get("Scheduled Automatic Index Update of {0}"), index.Name );
            switch (schedule.IntervalSize)
            {
                case IntervalSize.Day:
                    td.Triggers.Add(new DailyTrigger { DaysInterval = schedule.Interval, StartBoundary = schedule.StartingFrom });
                    break;
                case IntervalSize.Week:
                    td.Triggers.Add(new WeeklyTrigger { WeeksInterval = schedule.Interval, StartBoundary = schedule.StartingFrom });
                    break;
            }
            string exe_path = Assembly.GetExecutingAssembly().Location;
            td.Actions.Add(new ExecAction(exe_path, $"--scheduled {index.Id}"));
            return td;
        }
    }
}
