using GamingPlatform.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class GameServiceTests
    {
        private readonly TestDatabase _db;
        public GameServiceTests(TestDatabase db) => _db = db;

        [Fact]
        public async Task Only_the_studios_dev_or_an_admin_can_manage_a_game()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var games = sp.GetRequiredService<IGameService>();
            var dev = await TestData.UserAsync(sp);
            var otherDev = await TestData.UserAsync(sp);
            var game = games.GetGameById(TestData.Game(sp, devUserId: dev.Id).Id)!;

            Assert.True(games.CanManage(game, dev.Id, isAdmin: false));
            Assert.False(games.CanManage(game, otherDev.Id, isAdmin: false));
            Assert.False(games.CanManage(game, null, isAdmin: false));       // logged out
            Assert.True(games.CanManage(game, otherDev.Id, isAdmin: true));
        }

        [Fact]
        public async Task A_delisted_game_is_visible_only_to_owners_and_its_managers()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var games = sp.GetRequiredService<IGameService>();
            var dev = await TestData.UserAsync(sp);
            var owner = await TestData.UserAsync(sp);
            var stranger = await TestData.UserAsync(sp);
            var game = TestData.Game(sp, delisted: true, devUserId: dev.Id);
            TestData.Own(sp, owner.Id, game.Id);

            Assert.Null(games.GetDetails(game.Id, stranger.Id, isAdmin: false));
            Assert.Null(games.GetDetails(game.Id, null, isAdmin: false));
            Assert.NotNull(games.GetDetails(game.Id, owner.Id, isAdmin: false));
            Assert.NotNull(games.GetDetails(game.Id, dev.Id, isAdmin: false));
        }

        [Fact]
        public async Task A_dev_cant_edit_another_studios_game()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var games = sp.GetRequiredService<IGameService>();

            // TODO(human): create two devs, give the first one a game (TestData.Game with devUserId),
            // then have the SECOND dev call games.UpdateAsync(...) with a new title.
            // Assert the result's Status is ResultStatus.Forbidden, and that the title in the
            // database didn't change (check it through a fresh _db.NewScope()).
        }

        [Fact]
        public async Task Store_flags_are_for_the_viewer()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var games = sp.GetRequiredService<IGameService>();
            var viewer = await TestData.UserAsync(sp);
            var owned = TestData.Game(sp);
            var wished = TestData.Game(sp);
            TestData.Own(sp, viewer.Id, owned.Id);
            sp.GetRequiredService<IWishlistService>().Add(viewer.Id, wished.Id);

            var dtos = games.GetByIds(new[] { owned.Id, wished.Id }, viewer.Id, includeDelisted: false);

            Assert.Equal(new[] { owned.Id, wished.Id }, dtos.Select(d => d.Id)); // keeps the requested order
            Assert.True(dtos[0].InLibrary);
            Assert.True(dtos[1].InWishlist);
            Assert.False(games.GetByIds(new[] { owned.Id }, null, false)[0].InLibrary); // logged out
        }
    }
}
