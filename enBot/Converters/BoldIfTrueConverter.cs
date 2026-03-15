using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace enBot.Converters;

public class BoldIfTrueConverter : IValueConverter
{
    public static readonly BoldIfTrueConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FontWeight.Bold : FontWeight.Normal;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
