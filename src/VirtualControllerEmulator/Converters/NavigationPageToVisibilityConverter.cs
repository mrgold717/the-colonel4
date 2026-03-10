using System.Globalization;
using System.Windows;
using System.Windows.Data;
using VirtualControllerEmulator.ViewModels;

namespace VirtualControllerEmulator.Converters;

/// <summary>
/// Converts a NavigationPage enum value to Visibility.
/// Parameter must be the NavigationPage name (e.g. "Dashboard", "Mapping", etc.)
/// </summary>
[ValueConversion(typeof(NavigationPage), typeof(Visibility))]
public class NavigationPageToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is NavigationPage currentPage &&
            parameter is string pageName &&
            Enum.TryParse<NavigationPage>(pageName, out var targetPage))
        {
            return currentPage == targetPage ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
