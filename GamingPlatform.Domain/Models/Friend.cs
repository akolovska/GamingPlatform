using GamingPlatform.Domain.Enum;

namespace GamingPlatform.Domain.Models;

public class Friend : BaseEntity
{
    public string UserId { get; set; }        // requester
    public string FriendId { get; set; }      // receiver
    public FriendshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}