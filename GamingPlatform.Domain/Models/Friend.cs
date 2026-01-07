using GamingPlatform.Domain.Enum;

namespace GamingPlatform.Domain.Models;

public class Friend : BaseEntity
{
    public Guid UserId { get; set; }        // requester
    public Guid FriendId { get; set; }      // receiver

    public FriendshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}