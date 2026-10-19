namespace GamingPlatform.Domain.Enum;

public enum FriendRequestResult
{
    Sent,
    Accepted,          // they had already asked me, so we're now friends
    UserNotFound,      // also used when the target blocked you, so a block can't be detected
    Self,
    AlreadyFriends,
    AlreadyPending,
    YouBlocked
}
