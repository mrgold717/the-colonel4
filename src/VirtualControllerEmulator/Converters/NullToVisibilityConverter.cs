using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VirtualControllerEmulator.Converters;

/// <summary>
/// Converts null → Collapsed, non-null → Visible (or inverted with parameter "Invert")
/// </summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        bool invert = parameter?.ToString()?.Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
        bool show = invert ? !isNull : isNull;
        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
