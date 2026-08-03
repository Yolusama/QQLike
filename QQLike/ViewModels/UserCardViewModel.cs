using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QQLike.Components;
using QQLike.Entity;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration;
using QQLike.Entity.DTO;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;
using SqlSugar;

namespace QQLike.ViewModels;

public partial class UserCardViewModel(
    ISqlSugarClient sugarClient,
    ISessionStorage sessionStorage,
    SysSetting setting) : ViewModelBase<UserCard>
{
    [ObservableProperty] private string _avatar = string.Empty;
    [ObservableProperty] private string _nickname = string.Empty;
    [ObservableProperty] private string _account = string.Empty;
    [ObservableProperty] private string _signature = string.Empty;
    [ObservableProperty] private string _genderSymbol = string.Empty;
    [ObservableProperty] private string _onlineStatus = string.Empty;

    [ObservableProperty] private string _birthdayText = string.Empty;
    [ObservableProperty] private bool _showBirthday;

    [ObservableProperty] private string _locationText = string.Empty;
    [ObservableProperty] private bool _showLocation;

    [ObservableProperty] private string _groupNickname = string.Empty;
    [ObservableProperty] private bool _showGroupNickname;

    [ObservableProperty] private string _groupRoleText = string.Empty;
    [ObservableProperty] private bool _showGroupRole;
    [ObservableProperty] private bool _isFriend;
    [ObservableProperty] private bool _isNotFriend;

    [RelayCommand]
    private async Task LoadCard(UserCardDTO userCardDto)
    {
        var userCache = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        if (string.IsNullOrWhiteSpace(userCardDto.UserId))
        {
            ResetOptionalRows();
            return;
        }

        var user = await sugarClient.Queryable<User>().FirstAsync(u => u.Id == userCardDto.UserId);
        if (user is null)
        {
            ResetOptionalRows();
            return;
        }

        Nickname = user.Nickname ?? string.Empty;
        Account = $"QQLike {user.Account}";
        Signature = user.Signature ?? string.Empty;
        OnlineStatus = user.IsOnline == true ? "在线" : "离线";
        GenderSymbol = user.Gender == 0 ? "♂" : "♀";
        Avatar = BuildAvatar(user.Avatar);

        BirthdayText = user.Birthday?.ToString("yyyy-MM-dd") ?? string.Empty;
        ShowBirthday = !string.IsNullOrWhiteSpace(BirthdayText);

        var locationParts = new[] { user.Province, user.Region }
            .Where(static text => !string.IsNullOrWhiteSpace(text));
        LocationText = string.Join(" ", locationParts);
        ShowLocation = !string.IsNullOrWhiteSpace(LocationText);
        IsFriend = await sugarClient.Queryable<UserContact>().AnyAsync(uc =>
            uc.ContactId == userCardDto.UserId && uc.UserId == userCache.UserId && !uc.IsGroup);
        IsNotFriend = !IsFriend;

        await LoadGroupInfoAsync(userCardDto.UserId, userCardDto.InGroup, userCardDto.GroupId);
    }

    [RelayCommand]
    private async Task AddFriend()
    {
        
    }

    private async Task LoadGroupInfoAsync(string userId, bool inGroup, string? groupId)
    {
        ShowGroupNickname = false;
        GroupNickname = string.Empty;
        ShowGroupRole = false;
        GroupRoleText = string.Empty;

        if (!inGroup || string.IsNullOrWhiteSpace(groupId))
            return;

        var relation = await sugarClient.Queryable<UserContact>()
            .FirstAsync(uc => uc.IsGroup && uc.UserId == userId && uc.ContactId == groupId);

        if (!string.IsNullOrWhiteSpace(relation?.GroupDisplayName))
        {
            GroupNickname = relation.GroupDisplayName;
            ShowGroupNickname = true;
        }

        var group = await sugarClient.Queryable<ChatGroup>().FirstAsync(g => g.Id == groupId);
        if (group is not null && group.OwnerId == userId)
        {
            GroupRoleText = "群主";
            ShowGroupRole = true;
        }
    }

    private string BuildAvatar(string? avatarFile)
    {
        if (string.IsNullOrWhiteSpace(avatarFile))
            return string.Empty;

        return avatarFile.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? avatarFile
            : $"{setting.ApiUrl}/Files/Images/{avatarFile}";
    }

    private void ResetOptionalRows()
    {
        ShowBirthday = false;
        ShowLocation = false;
        ShowGroupNickname = false;
        ShowGroupRole = false;
    }
}