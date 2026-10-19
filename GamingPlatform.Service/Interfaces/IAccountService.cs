using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace GamingPlatform.Service.Interfaces
{
    public interface IAccountService
    {
        // ---------- Sign-up ----------

        // Creates the account (with the User role). Value = the new user.
        Task<ServiceResult<GamingPlatformUser>> RegisterAsync(RegisterDto dto, bool emailConfirmed = false);

        // First sign-in with Google: creates the account and links the Google login to it.
        // The email counts as confirmed only if it is the one Google vouched for.
        Task<ServiceResult<GamingPlatformUser>> RegisterExternalAsync(ExternalRegisterDto dto, ExternalLoginInfo info);

        // ---------- Profiles ----------

        ProfileDto? GetMyProfile(string userId);

        // null when the user doesn't exist or either of you blocked the other
        ProfileDto? GetProfile(string userName, string viewerId);

        ProfileEditDto? GetProfileForEdit(string userId);

        // Value = true when the email changed (it then needs confirming again)
        Task<ServiceResult<bool>> UpdateProfileAsync(string userId, ProfileEditDto dto);

        Task<ServiceResult> UpdateAvatarAsync(string userId, IFormFile? file);
        Task RemoveAvatarAsync(string userId);

        // buildCallbackUrl(userId, code) returns the confirmation link. Returns false if
        // the email is already confirmed or couldn't be sent.
        Task<bool> SendConfirmationEmailAsync(string userId, Func<string, string, string> buildCallbackUrl);

        // ---------- Security ----------

        Task<SecurityStatusDto?> GetSecurityStatusAsync(string userId);

        // Changes the password, or sets a first one for accounts made with Google (CurrentPassword is then ignored)
        Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordDto dto);

        Task<AuthenticatorSetupDto?> GetAuthenticatorSetupAsync(string userId, string issuer);
        Task<ServiceResult<List<string>>> EnableTwoFactorAsync(string userId, string code);    // Value = recovery codes
        Task<ServiceResult> DisableTwoFactorAsync(string userId);
        Task<ServiceResult<List<string>>> NewRecoveryCodesAsync(string userId);

        Task<ServiceResult> LinkExternalLoginAsync(string userId, ExternalLoginInfo info);
        Task<ServiceResult> RemoveExternalLoginAsync(string userId, string provider);

        // ---------- Deleting ----------

        // The user deletes their own account. confirmation = their password, or their username
        // if the account has no password (made with Google).
        Task<ServiceResult> DeleteAccountAsync(string userId, string confirmation);

        // An admin deletes someone (IUserAdminService checks who may)
        Task<ServiceResult> DeleteUserAsync(string userId);
    }
}
