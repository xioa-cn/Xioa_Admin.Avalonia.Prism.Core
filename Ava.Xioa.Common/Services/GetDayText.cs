using System;
using System.Collections.Generic;
using Ava.Xioa.Common.Extensions;

namespace Ava.Xioa.Common.Services;

public class GetDayText
{
    private static readonly Random Random = new Random();

    #region 分时段文案池
    // 深夜 0~6
    private static readonly List<string> _nightDeep = new()
    {
        "{name}，都凌晨了还在忙活，别硬扛，忙完早点躺平休息",
        "夜深啦{name}，还在坚守真的很拼，记得抽空喝口水缓一缓",
        "哈喽{name}，深夜安静适合沉淀，但别熬到身体吃不消哦",
        "午夜安康{name}，再努力也要留一点时间好好善待自己"
    };

    // 早上 6~11:30
    private static readonly List<string> _morning = new()
    {
        "早呀{name}，新的一天开启，先好好吃个早餐再开工",
        "早安{name}，清晨氛围超舒服，祝你今天一切顺顺利利",
        "嗨{name}，打起精神来，今天也会有很多小惊喜等着你",
        "早上好{name}，抛开昨日烦恼，轻装上阵好好度过今天"
    };

    // 中午 11:30~14:00
    private static readonly List<string> _noon = new()
    {
        "到饭点啦{name}，放下手头工作，好好吃顿午饭歇歇吧",
        "中午好{name}，别一直盯着屏幕，饭后稍微小憩一会更舒服",
        "哈喽{name}，忙碌一上午，给自己一点放空放松的时间",
        "正午安康{name}，补充好能量，下午干活才更有状态"
    };

    // 下午 14:00~18:30
    private static readonly List<string> _afternoon = new()
    {
        "下午好{name}，容易犯困就起身走动两分钟，舒展一下身体",
        "嗨{name}，午后不用太急躁，慢慢来，稳步做完手头的事就很棒",
        "午后安{name}，泡杯温水缓一缓，调整状态继续加油",
        "哈喽{name}，距离下班不远啦，稳住心态收尾好今日工作"
    };

    // 晚上 18:30~24
    private static readonly List<string> _evening = new()
    {
        "晚上好{name}，一天的忙碌告一段落，好好享受属于自己的时光",
        "辛苦啦{name}，抛开工作琐事，吃点好吃的犒劳一下自己",
        "天黑咯{name}，不用紧绷神经，放松下来安安静静歇一会儿",
        "晚间安康{name}，卸下所有疲惫，好好放松舒缓心情吧"
    };
    #endregion

    public static string ApplicationSayHello(string? useName)
    {
        string name = string.IsNullOrWhiteSpace(useName) ? "小伙伴" : useName;
        var nowHour = SystemTime.Now().Hour;
        var nowMinute = SystemTime.Now().Minute;
        List<string> textPool;

        textPool = (nowHour, nowMinute) switch
        {
            // 深夜 0 ~ 6
            (>=0, _) when nowHour < 6 => _nightDeep,
            // 早上 6:00 ~ 11:30
            (>=6, _) when nowHour < 11 => _morning,
            (11, <30) => _morning,
            // 中午 11:30 ~ 14:00
            (11, >=30) => _noon,
            (12, _) => _noon,
            (13, _) => _noon,
            (14, <0) => _noon,
            // 下午 14:00 ~ 18:30
            (>=14, _) when nowHour < 18 => _afternoon,
            (18, <30) => _afternoon,
            // 晚上 18:30 ~ 24
            _ => _evening
        };

        string randomStr = textPool[Random.Next(textPool.Count)];
        return randomStr.Replace("{name}", name);
    }
}