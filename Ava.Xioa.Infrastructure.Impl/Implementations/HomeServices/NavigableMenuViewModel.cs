using System;
using System.Collections.Generic;
using System.Linq;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Ava.Xioa.Infrastructure.Services.Services.RouterServices;
using Avalonia.Collections;
using Prism.Events;
using Prism.Navigation;
using Prism.Navigation.Regions;
using SukiUI.Controls;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.HomeServices;

[PrismViewModel(typeof(INavigableMenuServices))]
public partial class NavigableMenuViewModel : NavigableViewModelObject, INavigableMenuServices
{
    [ObservableBindProperty] private object? _selectedView;
    private readonly IToastsService _toastsService;
    public NavigableMenuViewModel(IEventAggregator? eventAggregator, IRegionManager regionManager,
        IRouterServices routerServices, IToastsService toastsService) : base(eventAggregator, regionManager)
    {
        _toastsService = toastsService;
        // var r= routerServices.PrismApplicationRouter();
        //  NavigableMenuItemModel[] menu =
        //  [
        //      new NavigableMenuItemModel("系统设置")
        //      {
        //          Header = "系统设置",
        //          IconKind = MaterialIconKind.VideoHomeSystem,
        //          NavigationName = "ThemesManager",
        //          Region = "HomeRegion",
        //          Children =
        //          [
        //              new NavigableMenuItemModel("主题设置")
        //              {
        //                  Header = "主题设置",
        //                  IconKind = MaterialIconKind.ThemeLightDark,
        //                  NavigationName = "ThemesManager",
        //                  Region = "HomeRegion",
        //              },
        //              new NavigableMenuItemModel("用户管理")
        //              {
        //                  Header = "用户管理",
        //                  IconKind = MaterialIconKind.AccountMultiple,
        //                  NavigationName = "UserManager",
        //                  Region = "HomeRegion"
        //              }
        //          ]
        //      },
        //      
        //  ];
        NavigableMenuItems = new AvaloniaList<NavigableMenuItemModel>(routerServices.PrismApplicationRouter());
    }

    public IAvaloniaReadOnlyList<NavigableMenuItemModel> NavigableMenuItems { get; }

    partial void OnSelectedViewChanged(object? value)
    {
        if (value is null) return;


        if (value is string valueKey)
        {
            GetKeyParameterExecuteNavigate(valueKey);

            return;
        }


        if (value is SukiSideMenuItem nav && nav.Tag is string key)
        {
            GetKeyParameterExecuteNavigate(key);
        }
    }

    private void GetKeyParameterExecuteNavigate(string key)
    {
        var findPage = FindMenuItemByKey(NavigableMenuItems, key);
        if (findPage is null || findPage.HasChildren) return;
        var navigationParameters = NavigationParametersHelper.TargetNavigationParametersWithHeader(
            findPage.NavigationName,
            findPage.Region, findPage.Header);
        ExecuteNavigate(navigationParameters);
    }


    private NavigableMenuItemModel? FindMenuItemByKey(
        IEnumerable<NavigableMenuItemModel>? menuItems,
        string targetKey)
    {
        if (menuItems == null) return null;

        foreach (var item in menuItems)
        {
            // 当前节点命中直接返回，替代 FirstOrDefault
            if (item.Key == targetKey)
                return item;

            // 递归搜子节点
            var childResult = FindMenuItemByKey(item.Children, targetKey);
            if (childResult is not null)
                return childResult;
        }

        // 所有层级均无匹配，返回 null
        return null;
    }

    protected override void OnNavigationFailed(NavigationContext? context, Exception? error)
    {
        _toastsService.ShowError("Error",$"页面导航失败 {error?.Message}");
    }
}