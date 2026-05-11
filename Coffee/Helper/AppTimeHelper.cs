namespace Coffee.Helper
{
    public static class AppTimeHelper
    {
        private static readonly TimeZoneInfo VietnamTimeZone = ResolveVietnamTimeZone();

        public static DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public static DateTimeOffset VietnamNow => TimeZoneInfo.ConvertTime(UtcNow, VietnamTimeZone);

        public static DateTimeOffset ToVietnamTime(DateTimeOffset value)
        {
            var utcValue = value.Offset == TimeSpan.Zero ? value : value.ToUniversalTime();
            return TimeZoneInfo.ConvertTime(utcValue, VietnamTimeZone);
        }

        private static TimeZoneInfo ResolveVietnamTimeZone()
        {
            var timezoneIds = new[]
            {
                "SE Asia Standard Time",
                "Asia/Ho_Chi_Minh",
                "Asia/Saigon"
            };

            foreach (var timezoneId in timezoneIds)
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return TimeZoneInfo.Utc;
        }
    }
}
