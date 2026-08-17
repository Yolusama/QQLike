using System.Globalization;
using System.Windows.Data;

namespace QQLike.Converters;

/// <summary>
/// 将未读数转换为显示文本：0 显示空，大于 99 显示 "99+"
/// </summary>
public class UnreadCountToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int count || count <= 0)
            return string.Empty;
        return count > 99 ? "99+" : count.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
