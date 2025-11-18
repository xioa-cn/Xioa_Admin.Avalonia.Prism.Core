using System;
using Ava.Xioa.Common.Modularity;
using Ava.Xioa.Common.Attributes;
using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Prism.Ioc;

namespace FlowModules;

[AutoModule(nameof(FlowModules))]
public class FlowModulesModule : PrismAutoModule<FlowModulesModule>
{
    // 模块核心功能实现

    public override void OnInitialized(IContainerProvider containerProvider)
    {
        base.OnInitialized(containerProvider);

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                // 基础 URI 使用当前模块的程序集（或任意有效基础路径，如 "resm:"）
                var styleInclude = new StyleInclude(new Uri("resm:"))
                {
                    // Source 明确指向资源所在的程序集和路径
                    Source = new Uri("avares://NodifyM.Avalonia/Styles/ControlStyles.axaml")
                };

                // 添加到全局资源字典
                if (!Application.Current.Resources.MergedDictionaries.Contains(styleInclude))
                {
                    Application.Current.Resources.MergedDictionaries.Add(styleInclude);
                }
            }
            catch (Exception ex)
            {
                // 捕获异常便于调试（如资源路径错误、程序集未引用等）
                Console.WriteLine($"加载样式失败: {ex.Message}");
            }
        }, DispatcherPriority.Input);
    }
}