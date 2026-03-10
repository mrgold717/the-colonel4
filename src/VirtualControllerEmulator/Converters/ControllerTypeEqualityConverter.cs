using System.Globalization;
using System.Windows.Data;
using VirtualControllerEmulator.Models;

namespace VirtualControllerEmulator.Converters;

/// <summary>
/// Converts ControllerType to bool by comparing against the bound parameter.
/// Use ConverterParameter=Xbox360 or ConverterParameter=DualShock4 on radio buttons.
/// </summary>
[ValueConversion(typeof(ControllerType), typeof(bool))]
public class ControllerTypeEqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ControllerType ct && parameter is string param)
        {
            if (Enum.TryParse<ControllerType>(param, out var target))
                return ct == target;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter is string param)
        {
            if (Enum.TryParse<ControllerType>(param, out var target))
                return target;
        }
        return System.Windows.DependencyProperty.UnsetValue;
    }
}
