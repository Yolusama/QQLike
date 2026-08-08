using CommunityToolkit.Mvvm.ComponentModel;

namespace QQLike.Domain;

public partial class UserContactGroupingItem : ObservableObject
{
    public long UserContactGroupId { get; set; }
    public string Name { get; set; }
    public long ContactCount { get; set; }
}