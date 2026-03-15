using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace enBot.Converters;

public class ItalicIfTrueConverter : IValueConverter
{
    public static readonly ItalicIfTrueConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FontStyle.Italic : FontStyle.Normal;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
