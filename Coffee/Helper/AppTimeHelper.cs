namespace Coffee.Helper
{
    public static class AppTimeHelper
    {
        // ===========================
        // 🕐 UTC
        // ===========================
        public static DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        // ===========================
        // 🌐 DYNAMIC
        // ===========================
        public static DateTimeOffset NowAt(string timezoneId)
        {
            var tz = ResolveTimeZone(timezoneId);
            return TimeZoneInfo.ConvertTime(UtcNow, tz);
        }

        public static DateTimeOffset ConvertTo(DateTimeOffset value, string timezoneId)
        {
            var tz = ResolveTimeZone(timezoneId);
            var utc = value.Offset == TimeSpan.Zero ? value : value.ToUniversalTime();
            return TimeZoneInfo.ConvertTime(utc, tz);
        }

        // ===========================
        // 🔧 INTERNAL
        // ===========================
        private static TimeZoneInfo ResolveTimeZone(params string[] timezoneIds)
        {
            foreach (var id in timezoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }
            return TimeZoneInfo.Utc;
        }
    }
}