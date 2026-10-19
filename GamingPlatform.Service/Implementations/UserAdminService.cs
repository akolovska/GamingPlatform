using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace GamingPlatform.Service.Implementations
{
    public class UserAdminService : IUserAdminService
    {
        public const int PageSize = 25;

        private readonly UserManager<GamingPlatformUser> _userManager;
        private readonly IAccountService _accountService;

        public UserAdminService(UserManager<GamingPlatformUser> userManager, IAccountService accountService)
        {
            _userManager = userManager;
            _accountService = accountService;
        }

        public async Task<PagedResult<AdminUserDto>> SearchAsync(string? query, int page, string adminId)
        {
            var text = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
            var users = _userManager.Users.Where(u => text == null || u.UserName!.Contains(text) || u.Email!.Contains(text));

            int total = users.Count();
            page = Math.Clamp(page, 1, Math.Max(1, (total + PageSize - 1) / PageSize));
            var pageOfUsers = users.OrderBy(u => u.UserName).Skip((page - 1) * PageSize).Take(PageSize).ToList();

            // Roles for the whole page in one go, instead of one query per user
            var roleNames = new Dictionary<string, HashSet<string>>();
            foreach (var role in new[] { Roles.Admin, Roles.Dev })
                roleNames[role] = (await _userManager.GetUsersInRoleAsync(role)).Select(u => u.Id).ToHashSet();

            var now = DateTimeOffset.UtcNow;
            return new PagedResult<AdminUserDto>
            {
                Page = page,
                PageSize = PageSize,
                TotalCount = total,
                Items = pageOfUsers.Select(u => new AdminUserDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email,
                    EmailConfirmed = u.EmailConfirmed,
                    Roles = roleNames.Where(r => r.Value.Contains(u.Id)).Select(r => r.Key).ToList(),
                    IsBanned = u.LockoutEnd != null && u.LockoutEnd > now.AddYears(100),
                    TwoFactorEnabled = u.TwoFactorEnabled,
                    IsSelf = u.Id == adminId
                }).ToList()
            };
        }

        // A ban is a lockout that never ends. Updating the security stamp signs them out everywhere
        // within a minute (see SecurityStampValidatorOptions in Program.cs).
        public async Task<ServiceResult> BanAsync(string userId, string adminId)
        {
            if (userId == adminId) return ServiceResult.Invalid("You can't ban yourself.");
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.NotFound();
            if (await _userManager.IsInRoleAsync(user, Roles.Admin)) return ServiceResult.Invalid("Remove their admin role first.");

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            await _userManager.UpdateSecurityStampAsync(user);
            return ServiceResult.Ok($"{user.UserName} is banned.");
        }

        public async Task<ServiceResult> UnbanAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.NotFound();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            return ServiceResult.Ok($"{user.UserName} is no longer banned.");
        }

        public async Task<ServiceResult> SetAdminAsync(string userId, bool makeAdmin, string adminId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResult.NotFound();

            bool isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
            if (isAdmin == makeAdmin) return ServiceResult.Ok("Nothing changed.");

            if (makeAdmin)
            {
                await _userManager.AddToRoleAsync(user, Roles.Admin);
            }
            else
            {
                // Someone else has to demote you, so there is always at least one admin left
                if (userId == adminId) return ServiceResult.Invalid("You can't remove your own admin role.");
                await _userManager.RemoveFromRoleAsync(user, Roles.Admin);
            }

            await _userManager.UpdateSecurityStampAsync(user); // their menus change at their next request
            return ServiceResult.Ok(makeAdmin ? $"{user.UserName} is now an admin." : $"{user.UserName} is no longer an admin.");
        }

        public async Task<ServiceResult> DeleteAsync(string userId, string adminId)
        {
            if (userId == adminId) return ServiceResult.Invalid("Delete your own account from Account settings instead.");
            var result = await _accountService.DeleteUserAsync(userId);
            return result.Succeeded ? ServiceResult.Ok("User deleted.") : result;
        }
    }
}
