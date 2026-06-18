using System;

namespace Ava.Xioa.Common.Themes.I18n;

public interface IAvaloniaI18Nable
{
}

public static class IAvaloniaI18nHelper
{
    public static string Tr(this IAvaloniaI18Nable avaloniaI18NHelper, string key)
    {
        return I18nManager.Instance.GetString(key);
    }

    public static string Tr(this IAvaloniaI18Nable avaloniaI18NHelper, string key, string defaultValue)
    {
        return I18nManager.Instance.GetString(key, defaultValue);
    }
}

public class CSharpI18n
{
    /// <summary>
    /// 后台使用使用I18n
    /// </summary>
    /// <returns></returns>
    public static (Func<string, string> Tr, string usingLang) UseI18n()
    {
        return (I18nManager.Instance.GetString, I18nManager.Instance.UsingLanguage);
    }

    public static (Func<string, string, string> Tr, string usingLang) UseI18nOfDefault()
    {
        return (I18nManager.Instance.GetString, I18nManager.Instance.UsingLanguage);
    }
}