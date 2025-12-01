using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ava.Xioa.InfrastructureModule.Views;

[PrismRegisterForNavigation(navigationName: nameof(TabItemDemo), region: AppRegions.MainRegion)]
public partial class TabItemDemo : UserControl
{
    public TabItemDemo()
    {
        InitializeComponent();
    }
}