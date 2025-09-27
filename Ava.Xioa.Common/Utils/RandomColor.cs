using System;

namespace Ava.Xioa.Common.Utils;

public class RandomColor
{
    public static string GenerateRandomColor()
    {
        var random = Random.Shared;
        return $"#{random.Next(0x1000000):X6}";
    }
}