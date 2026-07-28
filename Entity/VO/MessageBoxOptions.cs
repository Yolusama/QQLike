namespace QQLike.Entity.VO;

public class MessageBoxOptions
{
    public string Message { get; set; }
    public string Title { get; set; }
    public string ConfirmButtonText { get; set; } = "确定";
    public string CancelButtonText { get; set; } = "取消";
    public Action? ConfirmAction { get; set; } = null;
    public Action<string>? CancelAction { get; set; } = null;
}