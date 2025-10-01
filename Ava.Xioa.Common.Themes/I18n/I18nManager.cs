using Ava.Xioa.Common.Models;

namespace Ava.Xioa.Common.Themes.I18n;

public class I18nManager
{
    private static I18nManager? _i18NManager;

    public static I18nManager Instance
    {
        get
        {
            if (_i18NManager is null)
            {
                _i18NManager = new I18nManager();
            }

            return _i18NManager;
        }
    }


    public I18nJsonMode I18NJsonMode { get; set; }
    
    public string ResourceDirectory { get; private set; }
    
    public void I18nResourceDirectory(string directory)
    {
        ResourceDirectory = directory;
    }
}