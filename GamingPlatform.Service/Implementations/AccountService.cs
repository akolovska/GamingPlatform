using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace GamingPlatform.Service.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly IFriendService _friendService;
        private readonly ILibraryService _libraryService;
        private readonly IRepository<ReviewVote> _voteRepository;
        private readonly IImageService _images;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            UserManager<GamingPlatformUser> userManager,
            IFriendService friendService,
            ILibraryService libraryService,
            IRepository<ReviewVote> voteRepository,
            IImageService images,
            IUnitOfWork unitOfWork,
            IEmailSender emailSender,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _friendService = friendService;
            _libraryService = libraryService;
            _voteRepository = voteRepository;
            _images = images;
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _logger = logger;
        }

        // ---------- Sign-up ----------

        public async Task<ServiceResult<GamingPlatformUser>> RegisterAsync(RegisterDto dto, bool emailConfirmed = false)
        {
            var user = new GamingPlatformUser
            {
                UserName = dto.UserName.Trim(),
                Email = dto.Email.Trim(),
                EmailConfirmed = emailConfirmed,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                DateOfBirth = dto.DateOfBirth.Date,
                Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim()
            };

            // The user and their role are saved together
            var result = await _unitOfWork.InTransactionAsync(async () =>
            {
                var created = dto.Password == null
                    ? await _userManager.CreateAsync(user)                 // signs in with Google only
                    : await _userManager.CreateAsync(user, dto.Password);
                return created.Succeeded ? await _userManager.AddToRoleAsync(user, Roles.User) : created;
            }, commitIf: r => r.Succeeded);

            return result.Succeeded
                ? ServiceResult<GamingPlatformUser>.Success(user)
                : Errors<GamingPlatformUser>(result);
        }

        public async Task<ServiceResult<GamingPlatformUser>> RegisterExternalAsync(ExternalRegisterDto dto, ExternalLoginInfo info)
        {
            // Someone already has this email: they should sign in with their password and connect
            // Google from Account settings, instead of us silently merging two accounts
            if (await _userManager.FindByEmailAsync(dto.Email.Trim()) != null)
                return ServiceResult<GamingPlatformUser>.Invalid(
                    "An account with this email already exists. Log in with your password, then connect Google in Account settings.",
                    nameof(ExternalRegisterDto.Email));

            // Google only gives out verified addresses, so its email counts as confirmed. A different one doesn't.
            var googleEmail = info.Principal.FindFirstValue(ClaimTypes.Email);
            bool confirmed = googleEmail != null && string.Equals(googleEmail, dto.Email.Trim(), StringComparison.OrdinalIgnoreCase);

            return await _unitOfWork.InTransactionAsync(async () =>
            {
                var created = await RegisterAsync(new RegisterDto
                {
                    UserName = dto.Username,
                    Email = dto.Email,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    DateOfBirth = dto.DateOfBirth!.Value
                }, emailConfirmed: confirmed);
                if (!created.Succeeded) return created;

                var linked = await _userManager.AddLoginAsync(created.Value!, info);
                return linked.Succeeded ? created : Errors<GamingPlatformUser>(linked);
            }, commitIf: r => r.Succeeded);
        }

        // ---------- Profiles ----------

        public ProfileDto? GetMyProfile(string userId)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return null;

            return new ProfileDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Email = user.Email,
                IsVerified = user.EmailConfirmed,
                Friends = _friendService.GetFriends(user.Id),
                Games = _libraryService.GetLibrary(user.Id, user.Id),
                PendingRequests = _friendService.GetPendingRequests(user.Id),
                BlockedUsers = _friendService.GetBlockedUsers(user.Id)
            };
        }

        public ProfileDto? GetProfile(string userName, string viewerId)
        {
            var normalized = _userManager.NormalizeName(userName?.Trim() ?? string.Empty);
            var user = _userManager.Users.FirstOrDefault(u => u.NormalizedUserName == normalized);
            if (user == null) return null;
            if (user.Id == viewerId) return new ProfileDto { IsSelf = true };

            // A block in either direction hides the profile, the same way as a missing user
            if (_friendService.IsBlockedEitherWay(viewerId, user.Id)) return null;

            var friends = _friendService.GetFriends(user.Id);
            var pending = _friendService.GetPendingBetween(viewerId, user.Id);

            return new ProfileDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Bio = user.Bio,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Friends = friends,
                Games = _libraryService.GetLibrary(user.Id, viewerId),
                IsFriend = friends.Any(f => f.Id == viewerId),
                RequestSent = pending?.UserId == viewerId,                       // I asked them
                IncomingRequestId = pending?.FriendId == viewerId ? pending.Id : null, // they asked me
                CanViewWishlist = _friendService.CanViewWishlist(user, viewerId)
            };
        }

        public ProfileEditDto? GetProfileForEdit(string userId)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return null;

            return new ProfileEditDto
            {
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Bio = user.Bio,
                WishlistVisibility = user.WishlistVisibility,
                WishlistSaleEmails = user.WishlistSaleEmails
            };
        }

        public async Task<ServiceResult<bool>> UpdateProfileAsync(string userId, ProfileEditDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult<bool>.NotFound();

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();
            if (System.Enum.IsDefined(typeof(WishlistVisibility), dto.WishlistVisibility))
                user.WishlistVisibility = dto.WishlistVisibility;
            user.WishlistSaleEmails = dto.WishlistSaleEmails;

            var newEmail = dto.Email.Trim();
            bool emailChanged = !string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase);

            // SetEmailAsync also marks the email as unconfirmed, so a changed address has to be
            // verified again. It saves the other changes too.
            var result = emailChanged
                ? await _userManager.SetEmailAsync(user, newEmail)
                : await _userManager.UpdateAsync(user);

            if (!result.Succeeded) return Errors<bool>(result, nameof(ProfileEditDto.Email));

            return ServiceResult<bool>.Success(emailChanged, emailChanged
                ? "Profile saved. Please confirm your new email address."
                : "Profile saved.");
        }

        // ---------- Avatar ----------

        public async Task<ServiceResult> UpdateAvatarAsync(string userId, IFormFile? file)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.NotFound();

            var error = _images.Validate(file);
            if (error != null) return ServiceResult.Invalid(error);

            var oldUrl = user.ProfilePictureUrl;
            user.ProfilePictureUrl = await _images.SaveAsync(file!, ImageFolders.Avatars);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _images.Delete(user.ProfilePictureUrl, ImageFolders.Avatars);
                return Errors<object>(result);
            }

            _images.Delete(oldUrl, ImageFolders.Avatars);
            return ServiceResult.Ok("Profile picture updated.");
        }

        public async Task RemoveAvatarAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user?.ProfilePictureUrl == null) return;

            var oldUrl = user.ProfilePictureUrl;
            user.ProfilePictureUrl = null; // falls back to the default avatar
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) _images.Delete(oldUrl, ImageFolders.Avatars);
        }

        public async Task<bool> SendConfirmationEmailAsync(string userId, Func<string, string, string> buildCallbackUrl)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.EmailConfirmed || string.IsNullOrEmpty(user.Email)) return false;

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var url = buildCallbackUrl(user.Id, code);

            // The username is user input, so it's encoded before going into the HTML
            var body =
                $"Hello {HtmlEncoder.Default.Encode(user.UserName ?? string.Empty)},<br/><br/>" +
                $"Please confirm your email address by <a href='{HtmlEncoder.Default.Encode(url)}'>clicking here</a>.<br/><br/>" +
                "If you didn't create a GamingPlatform account, you can ignore this email.";

            try
            {
                await _emailSender.SendEmailAsync(user.Email, "GamingPlatform - Confirm your email", body);
                return true;
            }
            catch (Exception ex)
            {
                // A mail server problem shouldn't break registration or the profile page
                _logger.LogError(ex, "Could not send the confirmation email to user {UserId}", user.Id);
                return false;
            }
        }

        // ---------- Security ----------

        public async Task<SecurityStatusDto?> GetSecurityStatusAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return new SecurityStatusDto
            {
                HasPassword = await _userManager.HasPasswordAsync(user),
                TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user),
                LinkedProviders = (await _userManager.GetLoginsAsync(user)).Select(l => l.ProviderDisplayName ?? l.LoginProvider).ToList()
            };
        }

        public async Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.NotFound();

            // Accounts made with Google have no password yet; they can set one without a current one
            bool hasPassword = await _userManager.HasPasswordAsync(user);
            var result = hasPassword
                ? await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword)
                : await _userManager.AddPasswordAsync(user, dto.NewPassword);
            if (!result.Succeeded) return Errors<object>(result);

            return ServiceResult.Ok(hasPassword ? "Your password has been changed." : "Password set. You can now also log in with it.");
        }

        public async Task<AuthenticatorSetupDto?> GetAuthenticatorSetupAsync(string userId, string issuer)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            // A secret key shared with the authenticator app; created the first time it's needed
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var account = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id);
            var app = Uri.EscapeDataString(issuer);
            return new AuthenticatorSetupDto
            {
                // "ABCD EFGH ..." is easier to type into a phone than one long string
                SharedKey = string.Join(" ", key!.ToLowerInvariant().Chunk(4).Select(c => new string(c))),
                AuthenticatorUri = $"otpauth://totp/{app}:{account}?secret={key}&issuer={app}&digits=6"
            };
        }

        public async Task<ServiceResult<List<string>>> EnableTwoFactorAsync(string userId, string code)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult<List<string>>.NotFound();

            // Turning it on only after a correct code proves the app was set up right;
            // otherwise the user would lock themselves out
            var cleanCode = code.Replace(" ", string.Empty).Replace("-", string.Empty);
            bool valid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, cleanCode);
            if (!valid) return ServiceResult<List<string>>.Invalid("That code isn't right. Check the time on your phone and try a new code.", "Code");

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            return ServiceResult<List<string>>.Success(codes!.ToList(), "Two-factor login is on.");
        }

        public async Task<ServiceResult> DisableTwoFactorAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.NotFound();

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user); // the old app entry stops working
            return ServiceResult.Ok("Two-factor login is off.");
        }

        public async Task<ServiceResult<List<string>>> NewRecoveryCodesAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult<List<string>>.NotFound();
            if (!await _userManager.GetTwoFactorEnabledAsync(user))
                return ServiceResult<List<string>>.Invalid("Turn on two-factor login first.");

            // The old codes stop working
            var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            return ServiceResult<List<string>>.Success(codes!.ToList(), "New recovery codes created. The old ones no longer work.");
        }

        public async Task<ServiceResult> LinkExternalLoginAsync(string userId, ExternalLoginInfo info)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.NotFound();

            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
                return ServiceResult.Invalid($"That {info.ProviderDisplayName} account is already connected to another user.");
            return ServiceResult.Ok($"{info.ProviderDisplayName} connected. You can now log in with it.");
        }

        public async Task<ServiceResult> RemoveExternalLoginAsync(string userId, string provider)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.NotFound();

            var logins = await _userManager.GetLoginsAsync(user);
            var login = logins.FirstOrDefault(l => l.LoginProvider == provider);
            if (login == null) return ServiceResult.NotFound();

            // Never leave an account with no way to log in
            if (!await _userManager.HasPasswordAsync(user) && logins.Count == 1)
                return ServiceResult.Invalid("Set a password first, otherwise you couldn't log in any more.");

            var result = await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
            return result.Succeeded ? ServiceResult.Ok($"{login.ProviderDisplayName ?? provider} disconnected.") : Errors<object>(result);
        }

        // ---------- Deleting ----------

        public async Task<ServiceResult> DeleteAccountAsync(string userId, string confirmation)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.NotFound();

            // Asking again stops someone at an unlocked computer from deleting the account.
            // Accounts made with Google have no password, so they type their username instead.
            if (await _userManager.HasPasswordAsync(user))
            {
                if (!await _userManager.CheckPasswordAsync(user, confirmation))
                    return ServiceResult.Invalid("Incorrect password.", nameof(DeleteAccountDto.Password));
            }
            else if (!string.Equals(confirmation?.Trim(), user.UserName, StringComparison.OrdinalIgnoreCase))
            {
                return ServiceResult.Invalid("Type your username exactly to confirm.", nameof(DeleteAccountDto.Password));
            }

            if (await IsOnlyAdminAsync(user))
                return ServiceResult.Invalid("You are the only admin, so this account can't be deleted.");

            return await DeleteAsync(user);
        }

        public async Task<ServiceResult> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.NotFound();
            if (await IsOnlyAdminAsync(user)) return ServiceResult.Invalid("The only admin can't be deleted.");
            return await DeleteAsync(user);
        }

        private async Task<ServiceResult> DeleteAsync(GamingPlatformUser user)
        {
            // Friend rows reference the user twice and votes have no foreign key, so both go first.
            // The database handles the rest: library, wishlist, cart and notifications are deleted, reviews
            // stay with no author, a studio stays with its games, and orders stay as records.
            // One transaction: if deleting the user fails, everything comes back.
            var avatar = user.ProfilePictureUrl;
            var result = await _unitOfWork.InTransactionAsync(async () =>
            {
                _friendService.RemoveAllFor(user.Id);
                var votes = _voteRepository.GetAll(v => v, v => v.UserId == user.Id);
                if (votes.Count > 0) _voteRepository.DeleteRange(votes);
                return await _userManager.DeleteAsync(user);
            }, commitIf: r => r.Succeeded);
            if (!result.Succeeded) return Errors<object>(result);

            _images.Delete(avatar, ImageFolders.Avatars);
            return ServiceResult.Ok("The account has been deleted.");
        }

        private async Task<bool> IsOnlyAdminAsync(GamingPlatformUser user) =>
            await _userManager.IsInRoleAsync(user, Roles.Admin) &&
            (await _userManager.GetUsersInRoleAsync(Roles.Admin)).Count == 1;

        private static ServiceResult<T> Errors<T>(IdentityResult result, string field = "")
        {
            var failed = new ServiceResult<T>();
            foreach (var error in result.Errors)
                failed.AddError(field, error.Description);
            return failed;
        }
    }
}
