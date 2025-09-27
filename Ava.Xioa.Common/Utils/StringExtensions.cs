using System;
using System.Text.RegularExpressions;

namespace Ava.Xioa.Common.Utils;

public static class StringExtensions
{
    public static string MapPath(this string path)
    {
        var result = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

        if (!System.IO.Directory.Exists(result))
        {
            System.IO.Directory.CreateDirectory(result);
        }

        return result;
    }


    /// <summary>
    /// 移除字符串中的所有字母
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string RemoveLettersWithRegex(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // 匹配所有字母（大小写）并替换为空
        return Regex.Replace(input, "[A-Za-z]", "");
    }
}