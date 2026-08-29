namespace QQLike.Entity.VO;

public class ValueLabel<T>
{
    public T Value { get; set; }
    public string Label { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        var that = obj as ValueLabel<T>;
        return Value.Equals(that.Value);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}