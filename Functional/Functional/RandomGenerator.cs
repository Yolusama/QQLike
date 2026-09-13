using System.Text;
using QQLike.Entity.Common;
using QQLike.Entity.Enum;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class RandomGenerator : IRandomGenerator
{
    private const string alphabet ="0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string numbers = "0123456789";
    private const string letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private static readonly long[] bufferSizes =
    [
        4 * Constants.KB, 8 * Constants.KB, 16 * Constants.KB,
            32 * Constants.KB, 64 * Constants.KB, 128 * Constants.KB,
            256 * Constants.KB, 512 * Constants.KB, Constants.MB, 2 * Constants.MB,
            4 * Constants.MB, 8 * Constants.MB, 16 * Constants.MB, 32 * Constants.MB, 64 * Constants.MB,
            128 * Constants.MB, 256 * Constants.MB
    ];

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

    /// <summary>
    /// 模拟网络速度变化在随机区间生成当前buffer大小，保证buffer大小不小于size
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public long RandomBufferSize(long size)
    {
        var range = bufferSizes.Where(x => x >= size).ToArray();
        var index = Random.Shared.Next(0, range.Length);
        return range[index];
    }

    public string Guid => System.Guid.NewGuid().ToString();
}