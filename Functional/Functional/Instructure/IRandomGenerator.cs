using QQLike.Entity.Enum;

namespace QQLike.Functional.Instructure;

public interface IRandomGenerator
{
    public string GenerateByNumbers(int count,bool canStartWithZero = false);
    public string GenerateByLetters(int count, bool ignoreCase);
    public string GenerateByAlphabet(int count, LetterCaseOptions caseOptions = LetterCaseOptions.None);
    /// <summary>
    /// 模拟网络速度变化在随机区间生成当前buffer大小，保证buffer大小不小于size
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    public long RandomBufferSize(long size);
    public string Guid { get; }

}