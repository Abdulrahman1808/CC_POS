using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace POSSystem.Converters;

/// <summary>
/// Converts string to initials (first 1-2 letters of first/last name).
/// </summary>
public class InitialsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string name || string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length >= 2)
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        
        return parts[0].Length >= 2 
            ? $"{parts[0][0]}{parts[0][1]}".ToUpper()
            : parts[0][0].ToString().ToUpper();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts PIN length to dot fill color.
/// Parameter is the dot index (0-3).
/// </summary>
public class PinDotConverter : IValueConverter
{
    private static readonly SolidColorBrush FilledBrush = new(Color.FromRgb(99, 102, 241)); // #6366F1
    private static readonly SolidColorBrush EmptyBrush = new(Color.FromRgb(71, 85, 105));  // #475569

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int length && parameter is string indexStr && int.TryParse(indexStr, out var index))
        {
            return length > index ? FilledBrush : EmptyBrush;
        }
        return EmptyBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
