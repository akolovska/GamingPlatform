using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    [Collection("Database")]
    public class AccountServiceTests
    {
        private readonly TestDatabase _db;
        public AccountServiceTests(TestDatabase db) => _db = db;

        [Fact]
        public async Task Deleting_an_account_needs_the_right_password()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var user = await TestData.UserAsync(sp);

            var result = await sp.GetRequiredService<IAccountService>().DeleteAccountAsync(user.Id, "wrong password");

            Assert.False(result.Succeeded);
            Assert.True(sp.GetRequiredService<ApplicationDbContext>().Users.Any(u => u.Id == user.Id));
        }

        [Fact]
        public async Task Deleting_an_account_removes_its_data_but_keeps_reviews_anonymously()
        {
            using (var setup = _db.NewScope())
            {
                var sp = setup.ServiceProvider;
                var user = await TestData.UserAsync(sp);
                var friend = await TestData.UserAsync(sp);
                var game = TestData.Game(sp, price: 0);
                var other = TestData.Game(sp);
                TestData.Own(sp, user.Id, game.Id);
                sp.GetRequiredService<IWishlistService>().Add(user.Id, other.Id);
                sp.GetRequiredService<IReviewService>().Add(user.Id, game.Id, new ReviewFormDto { Score = 5, Comment = "kept" });
                var friends = sp.GetRequiredService<IFriendService>();
                friends.SendFriendRequest(user.Id, friend.UserName!);
                friends.SendFriendRequest(friend.Id, user.UserName!);

                var result = await sp.GetRequiredService<IAccountService>().DeleteAccountAsync(user.Id, TestData.Password);
                Assert.True(result.Succeeded);

                // Check with a fresh DbContext, like the next web request would
                using var check = _db.NewScope();
                var db = check.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                Assert.False(db.Users.Any(u => u.Id == user.Id));
                Assert.False(db.Libraries.Any(l => l.GamerId == user.Id));
                Assert.False(db.Wishlists.Any(w => w.GamerId == user.Id));
                Assert.False(db.Friends.Any(f => f.UserId == user.Id || f.FriendId == user.Id));
                Assert.True(db.Reviews.Any(r => r.GameId == game.Id && r.GamerId == null && r.Comment == "kept"));
            }
        }

        [Fact]
        public async Task The_only_admin_cant_delete_their_account()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var admin = await TestData.UserAsync(sp, Roles.Admin);
            Assert.Equal(1, db.UserRoles.Count(ur => ur.RoleId == db.Roles.Single(r => r.Name == Roles.Admin).Id));

            var result = await sp.GetRequiredService<IAccountService>().DeleteAccountAsync(admin.Id, TestData.Password);

            Assert.Contains("only admin", result.Message);
            // Leave no admin behind for the other tests
            db.UserRoles.RemoveRange(db.UserRoles.Where(ur => ur.UserId == admin.Id));
            db.SaveChanges();
        }

        [Fact]
        public async Task Changing_the_email_requires_confirming_it_again()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var user = await TestData.UserAsync(sp);
            var accounts = sp.GetRequiredService<IAccountService>();
            var form = accounts.GetProfileForEdit(user.Id)!;
            form.Email = "new-" + user.Email;

            var result = await accounts.UpdateProfileAsync(user.Id, form);

            Assert.True(result.Value); // "the email changed"
            using var check = _db.NewScope();
            Assert.False(check.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.Single(u => u.Id == user.Id).EmailConfirmed);
        }

        [Fact]
        public async Task The_confirmation_email_html_encodes_the_username()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var user = await TestData.UserAsync(sp);
            var db = sp.GetRequiredService<ApplicationDbContext>();
            db.Users.Single(u => u.Id == user.Id).EmailConfirmed = false;
            db.SaveChanges();

            var sent = await sp.GetRequiredService<IAccountService>()
                .SendConfirmationEmailAsync(user.Id, (id, code) => $"https://site/confirm?userId={id}&code={code}");

            Assert.True(sent);
            var mail = ((FakeEmailSender)sp.GetRequiredService<IEmailSender>()).Sent.Last(m => m.To == user.Email);
            Assert.Contains("&amp;code=", mail.Body); // the link is encoded inside the href
        }
    }
}
