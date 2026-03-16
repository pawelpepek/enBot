using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace enBot.Converters;

public class ScoreToColorConverter : IValueConverter
{
    public static readonly ScoreToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int score)
        {
            return score >= 8
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80))   // green
                : score >= 5
                    ? new SolidColorBrush(Color.FromRgb(255, 193, 7))  // amber
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54)); // red
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
