using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace enBot.Converters;

public class ComplexityToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int complexity && complexity > 7)
            return new SolidColorBrush(Color.Parse("#4CAF50"));
        return new SolidColorBrush(Color.Parse("#78909C"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
