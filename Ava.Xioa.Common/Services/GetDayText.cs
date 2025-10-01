using System;

namespace Ava.Xioa.Common.Services;

public class GetDayText
{
    public static string ApplicationSayHello(string useName)
    {
        var hour = DateTime.Now.Hour;
        return hour switch
        {
            >= 0 and < 6 => $"凌晨好，{useName}，深夜的努力终会有回响！",
            >= 6 and < 12 => $"早上好，{useName}，新的一天从元气满满的状态开始吧！",
            >= 12 and < 18 => $"下午好，{useName}，午后的时光慢慢走，继续做好手头的事就很棒～",
            _ => $"晚上好，{useName}，忙碌了一天，记得给自己留些放松的时间呀～"
        };
    }
}