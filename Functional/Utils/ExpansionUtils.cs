using System.Security.Cryptography;
using System.Text;
using QQLike.Entity.VO;

namespace QQLike.Functional.Utils;

public static class ExpansionUtils
{
    public static string ToSha256Str(this string str)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(str);
        var hashBytes = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", "");
    }
    
    public static int GetValue(this Enum enumParam)
    {
        var obj = (object)enumParam;
        return (int)obj;
    }
    
    public static T2 MapTo<T1, T2>(this T1 src, T2 dest)
    {
        // Map properties from src to instance
        var destType = typeof(T2);
        var properties = typeof(T1).GetProperties();
        foreach (var property in properties)
        {
            var destProperty = destType.GetProperty(property.Name);
            if (destProperty != null && destProperty.PropertyType.IsAssignableFrom(property.PropertyType))
            {
                var value = property.GetValue(src);
                destProperty.SetValue(dest, value);
            }
        }
        return dest;
    }

   
}