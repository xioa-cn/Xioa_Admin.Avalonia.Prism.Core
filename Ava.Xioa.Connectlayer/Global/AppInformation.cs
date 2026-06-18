using Ava.Xioa.Common.Themes.I18n;

namespace Ava.Xioa.Connectlayer.Global;

public class AppInformation : IAvaloniaI18Nable
{
    private static AppInformation? _instance;

    public static AppInformation Instance
    {
        get { return _instance ??= new AppInformation(); }
    }

    public string ApplicationName => this.Tr("Application", "Application");

    public string SplashIndexView => AvaRouter.LoginView;
}