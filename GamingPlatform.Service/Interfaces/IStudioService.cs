using GamingPlatform.Domain.DTO;

namespace GamingPlatform.Service.Interfaces
{
    // Public studio pages and the Dev dashboard (studio management for admins is IDevService)
    public interface IStudioService
    {
        // null = no such studio
        StudioPageDto? GetPage(Guid id, string? viewerId, bool isAdmin);

        // A Dev sees their own studio; an Admin any studio (studioId, or the first one).
        // Forbidden = a Dev asking for someone else's studio, Invalid = a Dev without a studio.
        ServiceResult<StudioDashboardDto> GetDashboard(Guid? studioId, string userId, bool isAdmin);
    }
}
