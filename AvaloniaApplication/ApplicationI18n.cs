using System.Reflection;
using Ava.Xioa.Common.Themes.I18n;

namespace AvaloniaApplication;

public partial class App
{
    public static void ConfigureLangManager()
    {
        // 语言文件为程序资源的写法
        {
            // 配置语言管理器使用当前程序集作为资源来源
            I18nManager.Instance.I18nResourceAssembly(Assembly.GetExecutingAssembly());
            // 设置资源在当前程序集中的命名空间
            I18nManager.Instance.I18nResourceNamespace("AvaloniaApplication");
            // 设置默认语言资源文件 （这里为资源名称 排除后命名空间）
            I18nManager.Instance.DefaultLang("Langs.ZH_CN");
        }
        // 语言文件为外部文件的写法
        // {
        //     // 先设置为外部资源模式 JsonMode 默认为 OnApplicationResources
        //     I18nManager.Instance.JsonMode(I18nJsonMode.OnFileDir);
        //     // 设置资源文件所在的目录
        //     I18nManager.Instance.I18nResourceDirectory(
        //         System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Langs")
        //     );
        //     // 设置默认语言文件
        //     I18nManager.Instance.DefaultLang("zh");
        // }


        // 初始化语言管理器
        I18nManager.Instance.Initialize();
    }
}