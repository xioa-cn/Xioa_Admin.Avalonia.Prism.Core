using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Avalonia.Controls;
using FlowModules.ViewModels;

namespace FlowModules.Views;

[PrismRegisterForNavigation(navigationName: nameof(FlowView), region: AppRegions.MainRegion)]
public partial class FlowView : UserControl
{
    public FlowView(FlowViewModel flowViewModel)
    {
        this.DataContext = flowViewModel;
        InitializeComponent();
    }
}