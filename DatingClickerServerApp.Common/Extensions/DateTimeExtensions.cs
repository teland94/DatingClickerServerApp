namespace DatingClickerServerApp.Common.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly TimeZoneInfo CurrentTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

        public static DateTime GetStartOfDay(this DateTime dateTime, bool isUtc = true)
        {
            return GetStartOfPeriod(dateTime, isUtc, PeriodType.Day);
        }

        public static DateTime GetStartOfWeek(this DateTime dateTime, bool isUtc = true)
        {
            return GetStartOfPeriod(dateTime, isUtc, PeriodType.Week);
        }

        public static DateTime GetStartOfMonth(this DateTime dateTime, bool isUtc = true)
        {
            return GetStartOfPeriod(dateTime, isUtc, PeriodType.Month);
        }

        public static DateTime GetStartOfQuarter(this DateTime dateTime, bool isUtc = true)
        {
            return GetStartOfPeriod(dateTime, isUtc, PeriodType.Quarter);
        }

        public static DateTime GetStartOfYear(this DateTime dateTime, bool isUtc = true)
        {
            return GetStartOfPeriod(dateTime, isUtc, PeriodType.Year);
        }

        public static DateTime ConvertToLocalTime(this DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, CurrentTimeZone);
        }

        private static DateTime GetStartOfPeriod(DateTime dateTime, bool isUtc, PeriodType periodType)
        {
            DateTime localDateTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, CurrentTimeZone);
            DateTime startOfPeriod = periodType switch
            {
                PeriodType.Day => new DateTime(localDateTime.Year, localDateTime.Month, localDateTime.Day, 0, 0, 0),
                PeriodType.Week => new DateTime(localDateTime.AddDays(-(7 + (localDateTime.DayOfWeek - DayOfWeek.Monday)) % 7).Year, localDateTime.Month, localDateTime.Day, 0, 0, 0),
                PeriodType.Month => new DateTime(localDateTime.Year, localDateTime.Month, 1, 0, 0, 0),
                PeriodType.Quarter => new DateTime(localDateTime.Year, ((localDateTime.Month - 1) / 3) * 3 + 1, 1, 0, 0, 0),
                PeriodType.Year => new DateTime(localDateTime.Year, 1, 1, 0, 0, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(periodType))
            };

            return isUtc ? DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(startOfPeriod, CurrentTimeZone), DateTimeKind.Utc) : DateTime.SpecifyKind(startOfPeriod, DateTimeKind.Local);
        }

        private enum PeriodType
        {
            Day,
            Week,
            Month,
            Quarter,
            Year
        }
    }
}
