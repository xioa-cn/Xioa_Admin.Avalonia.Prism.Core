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
    }
}