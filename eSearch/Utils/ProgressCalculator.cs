using System;
using S = eSearch.ViewModels.TranslationsViewModel;

namespace ProgressCalculation
{
    /// <summary>
    /// Generic Progress Calculation Methods.
    /// Generic Class Library by Tom
    /// </summary>
    public class ProgressCalculator
    {
        /// <summary>
        /// Get Progress
        /// </summary>
        /// <param name="x">Current</param>
        /// <param name="y">Total</param>
        /// <returns>X as a percentage of Y, if the value is between 0 or 100, 0 if less than 0, and 100 if more than 100.</returns>
        public static int GetXAsPercentOfY(double x, double y)
        {

            int result = Convert.ToInt32(Math.Round((x / y) * 100d));
            //MessageBox.Show("%"+result);
            if (result < 0) return 0;
            if (result < 100) return result; // rounding integers could do a funny but no need for precision here
            return 100;

        }

        /// <summary>
        /// Get Precise Progress
        /// </summary>
        /// <param name="x">Current</param>
        /// <param name="y">Total</param>
        /// <returns>X as a percentage of Y, if the value is between 0 or 100, 0 if less than 0, and 100 if more than 100.</returns>
        public static double GetXAsPercentOfYPrecise(double x, double y)
        {
            double result = ((x / y) * 100d);
            if (result < 0) return 0;
            if (result < 100) return result;
            return 100;
        }

        /// <summary>
        /// Calculate a Percentage between two Percents
        /// </summary>
        /// <param name="percentBetween">Percentage 'between' the two percents</param>
        /// <param name="percentLower">The lower percent</param>
        /// <param name="percentUpper">The upper percent</param>
        /// <returns>The percentage between the percents, or 0 if less than 0, or 100 if greater than 100.
        /// For example, if PercentBetween is 50, Percent Lower is 10 and Percent Upper is 20, the result will be 15 because 15 is 50% between 10 and 20.
        /// </returns>
        public static int getPercentBetweenPercents(int percentBetween, int percentLower, int percentUpper)
        {
            // Sanity Check
            if (percentUpper < percentLower)
            {
                throw (new InvalidOperationException("Percent upper cannot be lower than percentLower"));
            }

            double difference = (double)percentUpper - (double)percentLower;

            int retVal = (percentLower + GetValueFromPercentageOfValue(percentBetween, difference));
            if (retVal < 0) return 0;
            if (retVal > 100) return 100; // just incase - prevents pointless crashes..
            return retVal;
        }

        /// <summary>
        /// Calculate a Precise Percentage between two Percents
        /// </summary>
        /// <param name="percentBetween">Percentage 'between' the two percents</param>
        /// <param name="percentLower">The lower percent</param>
        /// <param name="percentUpper">The upper percent</param>
        /// <returns>The percentage between the percents, or 0 if less than 0, or 100 if greater than 100.
        /// For example, if PercentBetween is 50, Percent Lower is 10 and Percent Upper is 20, the result will be 15 because 15 is 50% between 10 and 20.
        /// </returns>
        public static double GetPercentBetweenPercentsPrecise(double percentBetween, double percentLower, double percentUpper)
        {
            // Sanity Check
            if (percentUpper < percentLower)
            {
                throw (new InvalidOperationException("Percent upper cannot be lower than percentLower"));
            }

            double difference = percentUpper - percentLower;
            double result = (percentLower + GetValueFromPercentageOfValuePrecise(percentBetween, difference));
            if (result < 0) return 0;
            if (result > 100) return 100;
            return result;
        }

        /// <summary>
        /// Retrieve a percentage of Value
        /// </summary>
        /// <param name="percentOf">The percentage of the value to retrieve</param>
        /// <param name="value">The value</param>
        /// <returns>A percentage of value</returns>
        public static int GetValueFromPercentageOfValue(double percentOf, double value)
        {
            return int.Parse(Math.Round((value / 100) * percentOf).ToString());
        }

        /// <summary>
        /// Retrieve a precise percentage of Value
        /// </summary>
        /// <param name="percentOf">The percentage of the value to retrieve</param>
        /// <param name="value">The value</param>
        /// <returns>A percentage of value</returns>
        public static double GetValueFromPercentageOfValuePrecise(double percentOf, double value)
        {
            return (value / 100) * percentOf;
        }

        /// <summary>
        /// Gets the Time remaining as a human friendly string
        /// </summary>
        /// <param name="startTime">The time the task was started</param>
        /// <param name="progressPercent">The current progress percent</param>
        /// <returns>Human friendly string. Eg "About 3 minutes remaining"</returns>
        public static string GetHumanFriendlyTimeRemaining(DateTime startTime, int progressPercent)
        {
            TimeSpan elapsedTime = DateTime.Now - startTime;
            return GetHumanFriendlyTimeRemaining(elapsedTime, progressPercent);
        }

        /// <summary>
        /// Gets the Time remaining as a human friendly string
        /// </summary>
        /// <param name="elapsedTime">The amount of time elapsed since start</param>
        /// <param name="progressPercent">The current progress percent</param>
        /// <returns>Human friendly string. Eg "About 3 minutes remaining"</returns>
        public static string GetHumanFriendlyTimeRemaining(TimeSpan elapsedTime, int progressPercent)
        {
            if (progressPercent < 3 || elapsedTime.TotalSeconds < 3) return "Calculating...";
            TimeSpan timeRemaining = GetTimeRemaining(elapsedTime, progressPercent);
            int days = Convert.ToInt32(timeRemaining.TotalDays);
            if (days >= 1)
            {
                return "About " + Pluralize(days, "day") + " remaining";
            }
            int hours = Convert.ToInt32(timeRemaining.TotalHours);
            if (hours >= 1)
            {
                return "About " + Pluralize(hours, "hour") + " remaining";
            }
            int minutes = Convert.ToInt32(timeRemaining.TotalMinutes);
            if (minutes >= 1)
            {
                return "About " + Pluralize(minutes, "minute") + " remaining";
            }
            int seconds = Convert.ToInt32(timeRemaining.TotalSeconds);
            return "About " + Pluralize(seconds, "second") + " remaining";

        }

        public static string GetHumanFriendlyTimeRemainingLocalizablePrecise(DateTime startTime, double progressPercent)
        {
            TimeSpan elapsedTime = DateTime.Now - startTime;
            return GetHumanFriendlyTimeRemainingLocalizablePrecise(elapsedTime, progressPercent);
        }

        public static string GetHumanFriendlyTimeRemainingLocalizablePrecise(TimeSpan elapsedTime, double progressPercent)
        {
            if (progressPercent < 3 && elapsedTime.TotalSeconds < 20) return S.Get("Calculating...");
            TimeSpan timeRemaining = GetTimeRemainingPrecise(elapsedTime, progressPercent);
            int days = Convert.ToInt32(timeRemaining.TotalDays);
            if (days >= 1)
            {
                return String.Format(
                    S.Get("{0} day(s) remaining"), 
                    days);
            }
            int hours = Convert.ToInt32(timeRemaining.TotalHours);
            if (hours >= 1)
            {
                return String.Format( 
                    S.Get("{0} hour(s) remaining"), 
                    hours
                    );
            }
            int minutes = Convert.ToInt32(timeRemaining.TotalMinutes);
            if (minutes >= 1)
            {
                return String.Format(
                    S.Get("{0} minute(s) remaining"),
                    minutes
                    );
            }
            int seconds = Convert.ToInt32(timeRemaining.TotalSeconds);
            return String.Format(
                S.Get("{0} second(s) remaining"),
                seconds
            );
        }

        /// <summary>
        /// Gets the time remaining as a TimeSpan
        /// </summary>
        /// <param name="startTime">Time when task was started</param>
        /// <param name="progressPercent">Current progress percent</param>
        /// <returns>Timespan remaining</returns>
        public static TimeSpan GetTimeRemaining(DateTime startTime, int progressPercent)
        {
            TimeSpan elapsedTime = DateTime.Now - startTime;
            return GetTimeRemaining(elapsedTime, progressPercent);
        }

        /// <summary>
        /// Gets the time remaining as a TimeSpan
        /// </summary>
        /// <param name="elapsedTime">Total elapsed time</param>
        /// <param name="progressPercent">Current progress percent</param>
        /// <returns>Timespan remaining</returns>
        public static TimeSpan GetTimeRemaining(TimeSpan elapsedTime, int progressPercent)
        {
            double progressDecimal = (double)progressPercent / 100.0;
            double elapsedSeconds = elapsedTime.TotalSeconds;
            double totalTime = (1.0 / progressDecimal) * elapsedSeconds;
            double secondsRemaining = totalTime - elapsedSeconds;
            return TimeSpan.FromSeconds(secondsRemaining);
        }

        public static TimeSpan GetTimeRemainingPrecise(TimeSpan elapsedTime, double progressPercent)
        {
            double progressDecimal = progressPercent / 100.0;
            double elapsedSeconds = elapsedTime.TotalSeconds;
            double totalTime = (1.0 / progressDecimal) * elapsedSeconds;
            double secondsRemaining = totalTime - elapsedSeconds;
            return TimeSpan.FromSeconds(secondsRemaining);
        }

        static string Pluralize(int number, string unit)
        {
            string s = "";
            if (number > 1) s = "s";
            return number + " " + unit + s;
        }
    }
}