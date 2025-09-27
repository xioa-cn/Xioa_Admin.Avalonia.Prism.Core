using System;
using System.Globalization;
using System.IO;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Ava.Xioa.Common.Themes.Converter;

public class WindowIconToImageSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is WindowIcon icon)
        {
            // 获取第一个图标（WindowIcon可能包含多尺寸图标）
            using var stream = new MemoryStream();
            icon.Save(stream);
            stream.Position = 0;
            return new Bitmap(stream);
        }
        return null;
    }
    
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotSupportedException();
}
