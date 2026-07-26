using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QQLike.Converters;

public class BoolVisibilityConverter: IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolVal = value != null && (bool)value;
        return boolVal ? Visibility.Visible : Visibility.Hidden;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value == null)
            return Visibility.Hidden;
        var visibility = (Visibility)value;
        return visibility == Visibility.Visible;
    }
}