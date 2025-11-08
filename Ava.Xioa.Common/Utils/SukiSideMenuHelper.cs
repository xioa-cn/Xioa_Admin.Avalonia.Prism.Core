using System.Linq;
using Ava.Xioa.Common.Models;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Material.Icons;
using SukiUI.Controls;

namespace Ava.Xioa.Common.Utils;

public class SukiSideMenuHelper
{
    public static SukiSideMenu GetNavigableMenu(IAvaloniaReadOnlyList<NavigableMenuItemModel> items)
    {
        SukiSideMenu menu = new SukiSideMenu();
        menu.IsSearchEnabled = false;
        menu.UseCustomContent = true;

        SukiSideMenuItem item1 = new SukiSideMenuItem();
        item1.Classes.Add("Compact");
        item1.Header = "欢迎";
        item1.IsVisible = false;
       
        item1.Tag = "欢迎Welcome";
        item1.Icon = new Material.Icons.Avalonia.MaterialIcon
        {
            Kind = MaterialIconKind.HumanWelcome,
            Width = 24,
            Height = 24
        };
        menu.Items.Insert(0, item1);

        foreach (var item in items)
        {
            SukiSideMenuItem menuItem = new SukiSideMenuItem();
            menuItem.Classes.Add("Compact");
            menuItem.Header = item.Header;
            menuItem.Tag = item.Key;
            menuItem.Icon = new Material.Icons.Avalonia.MaterialIcon
            {
                Kind = item.IconKind,
                Width = 24,
                Height = 24
            };


            if (item.Children is not null)
            {
                var child = item.Children.Select(CreateMenuItem).ToArray();
                foreach (var c in child)
                {
                    menuItem.Items.Add(c);
                }
            }

            menu.Items.Add(menuItem);
        }

        return menu;
    }


    private static SukiSideMenuItem CreateMenuItem(NavigableMenuItemModel item)
    {
        SukiSideMenuItem menuItem = new SukiSideMenuItem();
        menuItem.Classes.Add("Compact");
        menuItem.Header = item.Header;
        menuItem.Tag = item.Key;
        menuItem.Icon = new Material.Icons.Avalonia.MaterialIcon
        {
            Kind = item.IconKind,
            Width = 24,
            Height = 24
        };
        menuItem.Bind(SelectingItemsControl.IsSelectedProperty,
            new Binding("IsSelected") { Source = item, Mode = BindingMode.TwoWay });
        if (item.Children is not null)
        {
            foreach (NavigableMenuItemModel child in item.Children)
            {
                menuItem.Items.Add(CreateMenuItem(child));
            }
        }

        return menuItem;
    }
}