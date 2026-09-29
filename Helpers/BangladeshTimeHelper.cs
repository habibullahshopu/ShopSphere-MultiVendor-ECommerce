namespace ShopSphere.Helpers
{
    public static class BangladeshTimeHelper
    {
        private static readonly TimeZoneInfo BangladeshTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");

        public static DateTime ToBangladeshTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                utcDateTime = DateTime.SpecifyKind(
                    utcDateTime,
                    DateTimeKind.Utc);
            }

            return TimeZoneInfo.ConvertTimeFromUtc(
                utcDateTime,
                BangladeshTimeZone);
        }
    }
}