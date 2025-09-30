using System;
using System.Text;

namespace Ava.Xioa.Common.Utils;

public static class AppVersionTimeHelper
{
    public static bool IsOnTimeDay(this DateTime date, DateTime otherTime) {
        return date.Year == otherTime.Year && date.Month == otherTime.Month && date.Day == otherTime.Day;
    }

    public static string TimeYearMonthDayHourString(this DateTime date) {
        var result = date.ToString("yyyyMMdd");
        var dt = SwapEndian(result );
        return dt + ".XA";
    }

    static string SwapEndian(string hexString) {
        if (hexString.Length != 8)
        {
            return "版本错误";
        }

        var result = new StringBuilder();

        for (int i = 0; i < hexString.Length; i += 2)
        {
            if (i == 0)
            {
                result.Append(int.Parse(hexString[i + 1].ToString()) == 0 ? "6" : hexString[i + 1]);
                result.Append(hexString[i]);
            }
            else
            {
                result.Append(int.Parse(hexString[i + 1].ToString()) == 0 ? "9" : hexString[i + 1]);
                result.Append(hexString[i]);
            }
        }

        return result.ToString();
    }
}