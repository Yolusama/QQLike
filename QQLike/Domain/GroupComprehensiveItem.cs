namespace QQLike.Domain;

public class GroupComprehensiveItem
{
    public string Name { get; set; }
    public string Avatar { get; set; }
    public string GroupNum { get; set; }
    public string Description { get; set; }
    public int CurrentCount { get; set; }
    public int TotalCount { get; set; }
    public DateTime? CreateTime { get; set; }
    public string TimeText => GetTimeText();

    private string GetTimeText()
    {
        var yearGap = DateTime.Now.Year - CreateTime.Value.Year;
        if (yearGap > 0)
            return $"建群{yearGap}年";
        var monthGap = DateTime.Now.Month - CreateTime.Value.Month;
        if (monthGap > 1)
            return $"建群{monthGap}月";
        var dayGap = DateTime.Now.Day - CreateTime.Value.Day;
        if(dayGap > 7)
            return $"建群{dayGap}天";
        return "最近建群";
    }
}