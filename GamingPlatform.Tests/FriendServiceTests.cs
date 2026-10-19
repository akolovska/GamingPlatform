using GamingPlatform.Domain.Enum;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class FriendServiceTests
    {
        private readonly TestDatabase _db;
        public FriendServiceTests(TestDatabase db) => _db = db;

        [Fact]
        public async Task Asking_someone_who_already_asked_you_makes_you_friends()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var friends = sp.GetRequiredService<IFriendService>();
            var a = await TestData.UserAsync(sp);
            var b = await TestData.UserAsync(sp);

            Assert.Equal(FriendRequestResult.Sent, friends.SendFriendRequest(a.Id, b.UserName!));
            Assert.Equal(FriendRequestResult.Accepted, friends.SendFriendRequest(b.Id, a.UserName!));

            Assert.Contains(friends.GetFriends(a.Id), f => f.Id == b.Id);
            Assert.Empty(friends.GetPendingRequests(a.Id));
        }

        [Fact]
        public async Task Being_blocked_looks_like_the_user_doesnt_exist()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var friends = sp.GetRequiredService<IFriendService>();
            var blocker = await TestData.UserAsync(sp);
            var blocked = await TestData.UserAsync(sp);
            friends.BlockUser(blocker.Id, blocked.Id);

            var result = friends.SendFriendRequest(blocked.Id, blocker.UserName!);

            Assert.Equal(FriendRequestResult.UserNotFound, result);
            Assert.Equal(FriendRequestResult.UserNotFound, friends.SendFriendRequest(blocked.Id, "nobody-" + Guid.NewGuid()));
        }

        [Fact]
        public async Task Blocking_a_friend_ends_the_friendship()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var friends = sp.GetRequiredService<IFriendService>();
            var a = await TestData.UserAsync(sp);
            var b = await TestData.UserAsync(sp);
            friends.SendFriendRequest(a.Id, b.UserName!);
            friends.SendFriendRequest(b.Id, a.UserName!);

            friends.BlockUser(a.Id, b.Id);

            Assert.Empty(friends.GetFriends(a.Id));
            Assert.Contains(friends.GetBlockedUsers(a.Id), u => u.Id == b.Id);
            Assert.True(friends.IsBlockedEitherWay(b.Id, a.Id));
        }

        [Fact]
        public async Task RemoveAllFor_deletes_rows_in_both_directions()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var friends = sp.GetRequiredService<IFriendService>();
            var a = await TestData.UserAsync(sp);
            var b = await TestData.UserAsync(sp);
            var c = await TestData.UserAsync(sp);
            friends.SendFriendRequest(a.Id, b.UserName!);   // a -> b
            friends.BlockUser(c.Id, a.Id);                  // c -> a

            friends.RemoveAllFor(a.Id);

            var db = sp.GetRequiredService<ApplicationDbContext>();
            Assert.Equal(0, db.Friends.Count(f => f.UserId == a.Id || f.FriendId == a.Id));
        }
    }
}
