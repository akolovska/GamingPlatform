namespace GamingPlatform.Domain.Enum;

public enum FriendRequestResult
{
    Sent,
    Accepted,          // they had already asked me, so we're now friends
    UserNotFound,
    Self,
    AlreadyFriends,
    AlreadyPending,
    YouBlocked,
    BlockedByThem
}
