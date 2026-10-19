using GamingPlatform.Domain.DTO;

namespace GamingPlatform.Service.Interfaces
{
    // Admin user management. adminId is the admin doing it: nobody can ban, demote or delete
    // themselves this way, and the last admin can't be removed.
    public interface IUserAdminService
    {
        Task<PagedResult<AdminUserDto>> SearchAsync(string? query, int page, string adminId);

        Task<ServiceResult> BanAsync(string userId, string adminId);
        Task<ServiceResult> UnbanAsync(string userId);
        Task<ServiceResult> SetAdminAsync(string userId, bool makeAdmin, string adminId);
        Task<ServiceResult> DeleteAsync(string userId, string adminId);
    }
}
