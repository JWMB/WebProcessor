namespace Common
{
    public static class DateTimeExtensions
    {
        private static DateTime refDate = new DateTime(1970, 1, 1);
        public static long ToUnixTimestamp(this DateTime dateTime) => (long)(dateTime - refDate).TotalMilliseconds;
    }
}
