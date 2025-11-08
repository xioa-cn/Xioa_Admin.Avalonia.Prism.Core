using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Impl.Implementations.WindowServices;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ava.Xioa.InfrastructureModule.Components;

public partial class NavigableMenuContent : UserControl
{
    public NavigableMenuContent()
    {
        InitializeComponent();
    }

    private void NavigationBarInfoModel_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn || btn.Tag is not NavigableBarInfoModel model) return;
        if (this.DataContext is not MainWindowViewModel viewModel) return;
        var navigationParameters =
            NavigationParametersHelper.TargetNavigationParameters(model.TargetView, model.RegionName);
        viewModel.ExecuteNavigate(navigationParameters);

        viewModel.EventAggregator?
            .GetEvent<NavigableReverseSelectionEvent>()
            .Publish(new TokenKeyPubSubEvent<ReverseSelectionPub>("ReverseSelection",
                new ReverseSelectionPub()
                {
                    Key = model.Name
                }));
    }
}