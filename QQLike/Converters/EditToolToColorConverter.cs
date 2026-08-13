using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using QQLike.ViewModels;

namespace QQLike.Converters;

/// <summary>
/// 根据当前选中的编辑工具返回对应颜色，用于高亮显示选中的工具按钮
/// </summary>
public class EditToolToColorConverter : IValueConverter
{
    private static readonly Brush ActiveColor = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));
    private static readonly Brush InactiveColor = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not EditTool currentTool || parameter is not string toolName)
            return InactiveColor;

        if (Enum.TryParse<EditTool>(toolName, true, out var targetTool) && currentTool == targetTool)
            return ActiveColor;

        return InactiveColor;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return default;
    }
}
