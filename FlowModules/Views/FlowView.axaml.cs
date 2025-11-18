using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Avalonia.Controls;
using Avalonia.Input;
using FlowModules.ViewModels;
using NodifyM;

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