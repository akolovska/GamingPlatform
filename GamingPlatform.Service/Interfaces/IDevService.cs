using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces
{
    // Studios. Linking a user to a studio gives them the Dev role; unlinking takes it away.
    public interface IDevService
    {
        Dev? GetById(Guid id);
        Dev? GetByUserId(string userId);
        List<StudioDto> GetAll();

        Task<ServiceResult> CreateAsync(string name, string? ownerUserName);
        Task<ServiceResult> SetOwnerAsync(Guid id, string? ownerUserName); // empty name = unlink
        Task<ServiceResult> DeleteAsync(Guid id);                          // only studios without games
    }
}
