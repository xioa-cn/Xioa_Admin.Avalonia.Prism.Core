using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Themes.I18n;

namespace AvaloniaApplication.Utils;

public static class LangUtils
{
    public static LangSource[] ApplicationLanguages()
    {
        var langs = new List<LangSource>();
        if (I18nManager.Instance.I18NJsonMode == I18nJsonMode.OnApplicationResources)
        {
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (var res in resources)
            {
                if (res.Contains("Langs"))
                {
                    var name = res.Replace(".json", "").Replace($"{nameof(AvaloniaApplication)}.", "")
                        .Replace("Langs.", "");
                    langs.Add(new LangSource
                    {
                        Name = name,
                        SourceKey = res,
                    });
                }
            }
        }
        else if (I18nManager.Instance.I18NJsonMode == I18nJsonMode.OnFileDir)
        {
            var files = Directory.GetFiles(I18nManager.Instance.ResourceDirectory, "*.json");
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                langs.Add(new LangSource
                    {
                        Name = name,
                        SourceKey = name,
                    }
                );
            }
        }


        return langs.ToArray();
    }
}