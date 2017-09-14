
namespace System
{
    public static class DateTimeExtensions
    {
        public static long ToTimeStamp(this System.DateTime self)
        {
            return DateTimeToStamp(self);
        }

        private static System.DateTime FromTimeStamp(this DateTime self, long timeStamp)
        {
            System.DateTime dtStart = new System.DateTime(1970, 1, 1);
            var tmp = timeStamp.ToString();
            if (tmp.Length == 10)
                timeStamp *= 10000000;
            else
                timeStamp *= 10000;
            TimeSpan toNow = new TimeSpan(timeStamp);
            return dtStart.Add(toNow);
        }

        public static long DateTimeToStamp(System.DateTime time)
        {
            System.DateTime startTime = new System.DateTime(1970, 1, 1).Add(TimeZoneInfo.Local.BaseUtcOffset); // 当地时区
            return (long)(time - startTime).TotalMilliseconds;
        }
        
        public static System.DateTime ToBeijingTime(this DateTime self)
        {
            return self.ToUniversalTime().AddHours(8);
        }
    }
}