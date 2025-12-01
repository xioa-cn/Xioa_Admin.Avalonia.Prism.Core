namespace Ava.Xioa.Common.Const;

public class AppInformation
{
    private static AppInformation? _instance;

    public static AppInformation Instance
    {
        get { return _instance ??= new AppInformation(); }
    }

    public string ApplicationName { get; set; } = "Application";
    
    public string SplashIndexView { get; set; } = "LoginView";
}