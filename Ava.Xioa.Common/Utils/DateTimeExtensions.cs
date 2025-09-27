using System;

namespace Ava.Xioa.Common.Utils;

public class DateTimeExtensions
{
    public static string GetSystemNowDayString()
    {
        return DateTimeExtensions.SystemNow().ToString("MM月dd日yyyy年");
    }
    
    public static DateTime SystemNow()
    {
        return DateTime.Now;
    }
    
    public static DateTime SystemTokenNow()
    {
        return DateTime.UtcNow;
    }
}