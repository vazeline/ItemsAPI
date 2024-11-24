using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Common.ExtensionMethods
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Logic taken from Excel (apparently).
        /// </summary>
        public static int GetDifferenceInYears(this DateTime startDate, DateTime endDate)
        {
            // Excel documentation says "COMPLETE calendar years in between dates"
            var years = endDate.Year - startDate.Year;

            // if the start month and the end month are the same
            // and the end day is less than the start day
            // or the end month is less than the start month
            if (startDate.Month == endDate.Month
                && (endDate.Day < startDate.Day
                    || endDate.Month < startDate.Month))
            {
                years--;
            }

            return years;
        }

        /// <summary>
        /// Logic taken from Microsoft.VisualBasic.dll DateDiff, apparently.
        /// </summary>
        public static int GetDifferenceInMonths(this DateTime startDate, DateTime endDate)
        {
            return ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
        }

        public static string ToISOString(this DateTime dt)
        {
            return dt.ToString("o");
        }

        public static DateTime SetUtcKindAndConvertToLocalTime(this DateTime dt)
        {
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
        }

        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            var elapsedTime = dateTime.ToUniversalTime() - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime AddMonthsWithSpecificDayOfMonth(this DateTime dt, int months, int dayOfMonth)
        {
            // test if adding a month with the given dayOfMonth results in a valid date
            // (it might not for example if you're on 30th Jan, then you'd get 30th Feb)
            // if not, then step back a day until valid
            var dtNextMonth = dt.AddMonths(months);
            DateTime parsed;

            while (!DateTime.TryParseExact(
                s: $"{dayOfMonth}/{dtNextMonth.Month}/{dtNextMonth.Year}",
                format: "d/M/yyyy",
                provider: null,
                style: System.Globalization.DateTimeStyles.None,
                result: out parsed))
            {
                dayOfMonth--;
            }

            return parsed;
        }
    }
}
