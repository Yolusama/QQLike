using QQLike.Entity.VO;

namespace QQLike.Functional.Utils;

public static class EnumHelper
{
    public static List<ValueLabel<int>> ToValueLabels<TEnum>() where TEnum : Enum
    {
        var type = typeof(TEnum);
        var values = Enum.GetValues(type);
        var valueLabels = new List<ValueLabel<int>>();
        foreach (var value in values)
        {
            var intValue = Convert.ToInt32(value);
            var label = Enum.GetName(type, value);
            valueLabels.Add(new ValueLabel<int> { Value = intValue, Label = label });
        }
        return valueLabels;
    }
}