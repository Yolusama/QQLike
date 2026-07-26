using QQLike.Entity.Enum;

namespace QQLike.Functional.Instructure;

public interface IRandomGenerator
{
    public string GenerateByNumbers(int count,bool canStartWithZero = false);
    public string GenerateByLetters(int count, bool ignoreCase);
    public string GenerateByAlphabet(int count, LetterCaseOptions caseOptions = LetterCaseOptions.None);
    public string Guid { get; }

}