using System.Security.Cryptography;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GamingPlatform.Tests
{
    // Two-factor login, Google-only accounts, admin user management, notifications
    [Collection("Database")]
    public class AccountSecurityTests
    {
        private readonly TestDatabase _db;
        public AccountSecurityTests(TestDatabase db) => _db = db;

        // What an authenticator app shows: RFC 6238 (TOTP), HMAC-SHA1 over 30-second steps
        private static string Totp(string base32Key)
        {
            var key = Base32Decode(base32Key);
            var step = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
            if (BitConverter.IsLittleEndian) Array.Reverse(step);
            var hash = new HMACSHA1(key).ComputeHash(step);
            int offset = hash[^1] & 0x0F;
            int code = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
            return (code % 1_000_000).ToString("D6");
        }

        private static byte[] Base32Decode(string text)
        {
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var bits = string.Concat(text.ToUpperInvariant().Where(c => c != '=' && c != ' ')
                .Select(c => Convert.ToString(alphabet.IndexOf(c), 2).PadLeft(5, '0')));
            return Enumerable.Range(0, bits.Length / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, 8), 2)).ToArray();
        }

        [Fact]
        public async Task Two_factor_turns_on_only_with_a_correct_code_and_gives_recovery_codes()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var accounts = sp.GetRequiredService<IAccountService>();
            var users = sp.GetRequiredService<UserManager<GamingPlatformUser>>();
            var user = await TestData.UserAsync(sp);

            var setup = await accounts.GetAuthenticatorSetupAsync(user.Id, "GamingPlatform");
            Assert.StartsWith("otpauth://totp/GamingPlatform:", setup!.AuthenticatorUri);

            Assert.False((await accounts.EnableTwoFactorAsync(user.Id, "000000")).Succeeded);

            var key = await users.GetAuthenticatorKeyAsync(user);
            var enabled = await accounts.EnableTwoFactorAsync(user.Id, Totp(key!));
            Assert.True(enabled.Succeeded);
            Assert.Equal(10, enabled.Value!.Count);
            Assert.True((await accounts.GetSecurityStatusAsync(user.Id))!.TwoFactorEnabled);

            // A recovery code works once
            Assert.True((await users.RedeemTwoFactorRecoveryCodeAsync(user, enabled.Value[0])).Succeeded);
            Assert.False((await users.RedeemTwoFactorRecoveryCodeAsync(user, enabled.Value[0])).Succeeded);

            await accounts.DisableTwoFactorAsync(user.Id);
            Assert.False((await accounts.GetSecurityStatusAsync(user.Id))!.TwoFactorEnabled);
        }

        [Fact]
        public async Task An_account_without_a_password_is_deleted_by_typing_its_username()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var accounts = sp.GetRequiredService<IAccountService>();
            var name = "g" + Guid.NewGuid().ToString("N")[..10];
            var created = await accounts.RegisterAsync(new Domain.DTO.RegisterDto
            {
                UserName = name, Email = $"{name}@example.com", FirstName = "G", LastName = "User", DateOfBirth = new DateTime(1990, 1, 1)
            }, emailConfirmed: true); // like a Google sign-up: no password

            Assert.False((await accounts.GetSecurityStatusAsync(created.Value!.Id))!.HasPassword);
            Assert.False((await accounts.DeleteAccountAsync(created.Value.Id, "not-my-name")).Succeeded);
            Assert.True((await accounts.DeleteAccountAsync(created.Value.Id, name.ToUpperInvariant())).Succeeded);
        }

        [Fact]
        public async Task Admins_can_ban_and_manage_users_but_not_themselves()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var admin = sp.GetRequiredService<IUserAdminService>();
            var users = sp.GetRequiredService<UserManager<GamingPlatformUser>>();
            var me = await TestData.UserAsync(sp, Roles.Admin);
            var player = await TestData.UserAsync(sp);

            Assert.False((await admin.BanAsync(me.Id, me.Id)).Succeeded);
            Assert.True((await admin.BanAsync(player.Id, me.Id)).Succeeded);
            Assert.True(await users.IsLockedOutAsync(await users.FindByIdAsync(player.Id) ?? player));
            Assert.True((await admin.SearchAsync(player.UserName, 1, me.Id)).Items.Single().IsBanned);

            Assert.True((await admin.UnbanAsync(player.Id)).Succeeded);
            Assert.True((await admin.SetAdminAsync(player.Id, true, me.Id)).Succeeded);
            Assert.False((await admin.BanAsync(player.Id, me.Id)).Succeeded);        // demote an admin before banning
            Assert.False((await admin.SetAdminAsync(me.Id, false, me.Id)).Succeeded); // can't demote yourself
            Assert.True((await admin.SetAdminAsync(player.Id, false, me.Id)).Succeeded);

            Assert.False((await admin.DeleteAsync(me.Id, me.Id)).Succeeded);
            Assert.True((await admin.DeleteAsync(player.Id, me.Id)).Succeeded);

            // Leave no admin behind for the other tests
            await users.RemoveFromRoleAsync(me, Roles.Admin);
        }

        [Fact]
        public async Task Friend_requests_send_notifications_and_reading_them_clears_the_count()
        {
            using var scope = _db.NewScope();
            var sp = scope.ServiceProvider;
            var notifications = sp.GetRequiredService<INotificationService>();
            var a = await TestData.UserAsync(sp);
            var b = await TestData.UserAsync(sp);

            TestData.MakeFriends(sp, a, b);

            Assert.Equal(1, notifications.UnreadCount(b.Id));   // "a sent you a friend request"
            Assert.Equal(1, notifications.UnreadCount(a.Id));   // "b accepted your friend request"
            Assert.Contains(b.UserName!, notifications.GetRecent(a.Id).Single().Message);

            notifications.MarkAllRead(a.Id);
            Assert.Equal(0, notifications.UnreadCount(a.Id));
        }
    }
}
