using System.Linq;
using System.Threading.Tasks;
using Ava.Xioa.Common.Events;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Connectlayer.Global;
using Ava.Xioa.Infrastructure.Impl.Implementations.WindowServices;
using Ava.Xioa.Infrastructure.Services.Services.ThemesServices;
using Ava.Xioa.InfrastructureModule.Views;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Prism.Navigation.Regions;

namespace Ava.Xioa.InfrastructureModule.Components;

public partial class NavigableMenuContent : UserControl
{
    public NavigableMenuContent()
    {
        InitializeComponent();
    }

    private async void NavToWindowClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: NavigableBarInfoModel navigationInfo })
        {
            return;
        }

        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var regionManager = ContainerLocatorUtils.GetService<IRegionManager>();
        if (!regionManager.Regions.ContainsRegionWithName(navigationInfo.RegionName))
        {
            return;
        }

        var sourceRegion = regionManager.Regions[navigationInfo.RegionName];
        var content = sourceRegion.ActiveViews.FirstOrDefault() ?? sourceRegion.Views.FirstOrDefault();
        if (content is null)
        {
            return;
        }

        var themesServices = ContainerLocatorUtils.GetService<IThemesServices>();
        var navWindow = new NavWindow(regionManager, themesServices, navigationInfo, content, RestoreWindowContentToMain);
        regionManager.AddToRegion(navWindow.RegionName, content);
        ActivateSingle(regionManager.Regions[navWindow.RegionName], content);

        await NavigateMainToFallbackAsync(regionManager, viewModel, navigationInfo);
        navWindow.Show();
    }

    private void NavigationBarInfoModel_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn || btn.Tag is not NavigableBarInfoModel model) return;
        if (DataContext is not MainWindowViewModel viewModel) return;
        var navigationParameters =
            NavigationParametersHelper.TargetNavigationParameters(model.TargetView, model.RegionName);
        viewModel.ExecuteNavigate(navigationParameters);

        PublishReverseSelection(model);
    }

    private async Task NavigateMainToFallbackAsync(
        IRegionManager regionManager,
        MainWindowViewModel viewModel,
        NavigableBarInfoModel poppedInfo)
    {
        var items = viewModel.NavigableBarInfos.ToList();
        var poppedIndex = items.IndexOf(poppedInfo);
        var fallback = poppedIndex > 0
            ? items[poppedIndex - 1]
            : items.FirstOrDefault(item => !ReferenceEquals(item, poppedInfo));

        viewModel.NavigableBarInfos.Remove(poppedInfo);

        foreach (var item in items)
        {
            item.IsCheck = false;
        }

        if (fallback is not null)
        {
            fallback.IsCheck = true;
            await regionManager.RequestNavigateAsync(fallback.RegionName, fallback.TargetView);
            PublishReverseSelection(fallback);
            return;
        }

        await regionManager.RequestNavigateAsync(poppedInfo.RegionName, AvaRouter.HomeWelcome);
        PublishReverseSelection(string.Empty);
    }

    private void RestoreWindowContentToMain(NavigableBarInfoModel navigationInfo, object content)
    {
        var regionManager = ContainerLocatorUtils.GetService<IRegionManager>();
        if (!regionManager.Regions.ContainsRegionWithName(navigationInfo.RegionName))
        {
            return;
        }

        regionManager.AddToRegion(navigationInfo.RegionName, content);
        ActivateSingle(regionManager.Regions[navigationInfo.RegionName], content);

        if (DataContext is MainWindowViewModel viewModel)
        {
            if (!viewModel.NavigableBarInfos.Contains(navigationInfo))
            {
                viewModel.NavigableBarInfos.Add(navigationInfo);
            }

            foreach (var item in viewModel.NavigableBarInfos)
            {
                item.IsCheck = ReferenceEquals(item, navigationInfo);
            }
        }

        PublishReverseSelection(navigationInfo);
    }

    private static void ActivateSingle(IRegion region, object content)
    {
        foreach (var activeView in region.ActiveViews.ToList())
        {
            if (!ReferenceEquals(activeView, content))
            {
                region.Deactivate(activeView);
            }
        }

        region.Activate(content);
    }

    private void PublishReverseSelection(NavigableBarInfoModel model)
    {
        PublishReverseSelection(string.IsNullOrWhiteSpace(model.SelectionKey) ? model.Name : model.SelectionKey);
    }

    private void PublishReverseSelection(string key)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.EventAggregator?
            .GetEvent<NavigableReverseSelectionEvent>()
            .Publish(new TokenKeyPubSubEvent<ReverseSelectionPub>("ReverseSelection",
                new ReverseSelectionPub
                {
                    Key = key
                }));
    }
}
