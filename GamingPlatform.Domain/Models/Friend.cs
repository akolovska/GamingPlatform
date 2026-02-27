using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;

namespace GamingPlatform.Domain.Models;

public class Friend : BaseEntity
{
    public string UserId { get; set; }
    public GamingPlatformUser User { get; set; }

    public string FriendId { get; set; }
    public GamingPlatformUser FriendUser { get; set; }
    public FriendshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public Friend(string userId, string friendId, FriendshipStatus status, DateTime createdAt)
    {
        UserId = userId;
        FriendId = friendId;
        Status = status;
        CreatedAt = createdAt;
    }
}