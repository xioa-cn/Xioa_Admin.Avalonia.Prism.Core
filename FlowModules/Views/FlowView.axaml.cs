using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    private void ResetViewClick(object? sender, RoutedEventArgs e)
    {
        Editor.ResetViewToDefault();
    }

    private void FitAllContentClick(object? sender, RoutedEventArgs e)
    {
        Editor.ZoomToFitAll();
    }
}