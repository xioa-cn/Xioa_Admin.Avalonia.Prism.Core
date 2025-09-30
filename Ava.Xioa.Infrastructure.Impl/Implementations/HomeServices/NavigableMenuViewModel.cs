using System.Collections.Generic;
using System.Linq;
using Ava.Xioa.Common;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Utils;
using Ava.Xioa.Infrastructure.Services.Services.HomeServices;
using Ava.Xioa.Infrastructure.Services.Services.RouterServices;
using Avalonia.Collections;
using Prism.Navigation.Regions;
using SukiUI.Controls;

namespace Ava.Xioa.Infrastructure.Impl.Implementations.HomeServices;

[PrismViewModel(typeof(INavigableMenuServices))]
public partial class NavigableMenuViewModel : NavigableViewModelObject, INavigableMenuServices
{
    [ObservableBindProperty] private object? _selectedView;
    
    public NavigableMenuViewModel(IRegionManager regionManager, IRouterServices routerServices) : base(regionManager)
    {
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

        if (value is string stringkey)
        {
            var findPage = FindMenuItemByKey(NavigableMenuItems, stringkey);

            if (findPage is not null && !findPage.HasChildren)
            {
                ExecuteNavigate(NavigationParametersHelper.TargetNavigationParameters(findPage.NavigationName,
                    findPage.Region));
            }

            return;
        }


        if (value is SukiSideMenuItem nav && nav.Tag is string key)
        {
            var findPage = FindMenuItemByKey(NavigableMenuItems, key);

            if (findPage is not null && !findPage.HasChildren)
            {
                ExecuteNavigate(NavigationParametersHelper.TargetNavigationParameters(findPage.NavigationName,
                    findPage.Region));
            }
        }
    }


    private NavigableMenuItemModel? FindMenuItemByKey(
        IEnumerable<NavigableMenuItemModel>? menuItems,
        string targetKey)
    {
        if (menuItems == null) return null;

        // 先查询当前层级（顶层或某一层子菜单）
        var matchItem = menuItems.FirstOrDefault(item => item.Key == targetKey);
        if (matchItem != null)
        {
            return matchItem; // 找到匹配项，直接返回
        }

        // 若当前层级无匹配，递归查询每个子菜单
        foreach (var item in menuItems)
        {
            var childMatch = FindMenuItemByKey(item.Children, targetKey);
            if (childMatch != null)
            {
                return childMatch; // 子菜单中找到匹配项，返回
            }
        }

        // 所有层级均无匹配，返回 null
        return null;
    }
}