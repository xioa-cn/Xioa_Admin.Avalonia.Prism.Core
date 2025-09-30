using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ava.Xioa.Common.Models;
using Avalonia.Data.Converters;
using SukiUI.Controls;

namespace Ava.Xioa.Common.Themes.Converter;

public class NavigableMenuItemConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NavigableMenuItemModel item && item.Children != null)
        {
            return item.Children.Select(CreateMenuItem).ToArray();
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private SukiSideMenuItem CreateMenuItem(NavigableMenuItemModel item)
    {
        SukiSideMenuItem menuItem = new SukiSideMenuItem();

        menuItem.Header = item.Header;
        menuItem.Icon = new Material.Icons.Avalonia.MaterialIcon
        {
            Kind = item.IconKind,
            Width = 24,
            Height = 24
        };

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