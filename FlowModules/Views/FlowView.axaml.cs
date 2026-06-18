using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Connectlayer.Global;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

    private void ResetViewClick(object? sender, RoutedEventArgs e)
    {
        // Editor.ResetViewToDefault();
    }

    private void FitAllContentClick(object? sender, RoutedEventArgs e)
    {
        // Editor.ZoomToFitAll();
    }
}