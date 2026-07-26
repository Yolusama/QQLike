using System.Text;
using QQLike.Entity.Enum;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class RandomGenerator : IRandomGenerator
{
    private const string alphabet ="0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string numbers = "0123456789";
    private const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    
    /// <summary>
    /// 数字级生成
    /// </summary>
    public string GenerateByNumbers(int count,bool canStartWithZero = false)
    {
        var builder = new StringBuilder();
        var i = 0;
        if (!canStartWithZero)
        {
            builder.Append(alphabet[Random.Shared.Next(1,numbers.Length)]);
            i++;
        }

        while (i < count)
        {
            var index = Random.Shared.Next(0, numbers.Length);
            builder.Append(numbers[index]);
            i++;
        }
        return builder.ToString();
    }

    /// <summary>
    /// 以字母表生成
    /// </summary>
    /// <param name="count"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public string GenerateByLetters(int count, bool ignoreCase)
    {
        var table = ignoreCase ? letters.Substring(0,26) : letters;
        var builder = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            var index = Random.Shared.Next(0, table.Length);
            builder.Append(table[index]);
        }
        return builder.ToString();
    }

    /// <summary>
    /// 字符表生成：字母+数字
    /// </summary>
    /// <param name="count"></param>
    /// <param name="caseOptions"></param>
    /// <exception cref="NotImplementedException"></exception>
    public string GenerateByAlphabet(int count,LetterCaseOptions caseOptions = LetterCaseOptions.None)
    {
        var table = caseOptions switch
        {
            LetterCaseOptions.None => alphabet,
            LetterCaseOptions.LowerCase => string.Concat(numbers, letters.AsSpan(0, 26)),
            LetterCaseOptions.UpperCase => string.Concat(numbers, letters.AsSpan(26, 26)),
            _ => throw new ArgumentOutOfRangeException(nameof(caseOptions), caseOptions, null)
        };
        
        var builder = new StringBuilder();
        for (int i = 0; i < count; i++)
        {
            var index = Random.Shared.Next(0, table.Length);
            builder.Append(table[index]);
        }
        return builder.ToString();
    }

    public string Guid => System.Guid.NewGuid().ToString();
}