using System.ComponentModel.DataAnnotations;
using GamingPlatform.Domain.Validation;

namespace GamingPlatform.Domain.DTO
{
    // The Security part of Account settings
    public class SecurityStatusDto
    {
        public bool HasPassword { get; set; }                 // false for accounts made with Google
        public bool TwoFactorEnabled { get; set; }
        public int RecoveryCodesLeft { get; set; }
        public List<string> LinkedProviders { get; set; } = new(); // e.g. "Google"
    }

    // What the authenticator app needs: the key to type in, or the otpauth:// link for the QR code
    public class AuthenticatorSetupDto
    {
        public string SharedKey { get; set; } = string.Empty;       // shown in groups of 4
        public string AuthenticatorUri { get; set; } = string.Empty;
    }

    public class TwoFactorCodeDto
    {
        [Required, StringLength(7, MinimumLength = 6), Display(Name = "Code from your app")]
        public string Code { get; set; } = string.Empty;
    }

    // A new account. Password is null for accounts created by signing in with Google.
    public class RegisterDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Bio { get; set; }
        public string? Password { get; set; }
    }

    // The extra details asked for the first time someone signs in with Google
    public class ExternalRegisterDto
    {
        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(50), Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(50), Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required, StringLength(30, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your date of birth.")]
        [DataType(DataType.Date), MinimumAge(18), Display(Name = "Date of birth")]
        public DateTime? DateOfBirth { get; set; }
    }

    // A row on the admin Users page
    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsBanned { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool IsSelf { get; set; }
    }
}
