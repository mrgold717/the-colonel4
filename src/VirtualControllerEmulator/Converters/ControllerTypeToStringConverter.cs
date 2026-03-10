using System.Globalization;
using System.Windows.Data;
using VirtualControllerEmulator.Models;

namespace VirtualControllerEmulator.Converters;

[ValueConversion(typeof(ControllerType), typeof(string))]
public class ControllerTypeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ControllerType ct ? ct switch
        {
            ControllerType.Xbox360 => "Xbox 360",
            ControllerType.DualShock4 => "DualShock 4",
            _ => value.ToString() ?? string.Empty
        } : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Xbox 360" => ControllerType.Xbox360,
            "DualShock 4" => ControllerType.DualShock4,
            _ => ControllerType.Xbox360
        };
    }
}
