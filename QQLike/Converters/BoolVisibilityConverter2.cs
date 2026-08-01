using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QQLike.Converters;

/// <summary>
/// 绑定到Collapsed值，使其不占空间
/// </summary>
public class BoolVisibilityConverter2:IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var boolValue = value != null && (bool)value;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Visibility visibility)
            return false;
        return visibility == Visibility.Visible;
    }
}