using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SystemColor = System.Drawing.Color;

namespace CircularMidiGenerator.Converters;

/// <summary>
/// Converts between System.Drawing.Color and Avalonia.Media.Color
/// </summary>
public class ColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SystemColor systemColor)
        {
            return Color.FromArgb(systemColor.A, systemColor.R, systemColor.G, systemColor.B);
        }
        
        return Colors.Transparent;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color avaloniaColor)
        {
            return SystemColor.FromArgb(avaloniaColor.A, avaloniaColor.R, avaloniaColor.G, avaloniaColor.B);
        }
        
        return SystemColor.Transparent;
    }
}

/// <summary>
/// Converts boolean values to colors (true = green, false = red)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? Colors.LimeGreen : Colors.Red;
        }
        
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}