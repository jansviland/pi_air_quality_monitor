namespace AirQuality.Common.Extensions;

public static class DateTimeExtensions
{
    public static long ToUnixTime(this DateTime dateTime)
    {
        return (long)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}