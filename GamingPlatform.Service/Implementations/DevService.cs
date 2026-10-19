using GamingPlatform.Domain.DTO;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace GamingPlatform.Service.Implementations
{
    public class DevService : IDevService
    {
        private readonly IRepository<Dev> _devRepository;
        private readonly IRepository<GameDev> _gameDevRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<GamingPlatformUser> _userManager;

        public DevService(
            IRepository<Dev> devRepository,
            IRepository<GameDev> gameDevRepository,
            IUnitOfWork unitOfWork,
            UserManager<GamingPlatformUser> userManager)
        {
            _devRepository = devRepository;
            _gameDevRepository = gameDevRepository;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public Dev? GetById(Guid id) =>
            _devRepository.Get(selector: d => d, predicate: d => d.Id == id);

        public Dev? GetByUserId(string userId) =>
            _devRepository.Get(selector: d => d, predicate: d => d.UserId == userId);

        public List<StudioDto> GetAll() =>
            _devRepository.GetAll(
                selector: d => new StudioDto
                {
                    Id = d.Id,
                    Name = d.StudioName ?? "Unnamed studio",
                    OwnerUserName = d.User != null ? d.User.UserName : null,
                    GameCount = d.GameDevs.Count
                },
                orderBy: q => q.OrderBy(d => d.StudioName));

        public async Task<ServiceResult> CreateAsync(string name, string? ownerUserName)
        {
            name = name?.Trim() ?? string.Empty;
            if (name.Length is 0 or > 100) return ServiceResult.Invalid("Enter a studio name (max 100 characters).");
            if (_devRepository.Any(d => d.StudioName == name)) return ServiceResult.Invalid("A studio with that name already exists.");

            var (owner, error) = await FindNewOwnerAsync(ownerUserName, studioId: null);
            if (error != null) return ServiceResult.Invalid(error);

            // The studio and the owner's Dev role are saved together
            await _unitOfWork.InTransactionAsync(async () =>
            {
                _devRepository.Insert(new Dev { StudioName = name, UserId = owner?.Id });
                if (owner != null) await GrantDevRoleAsync(owner);
                return true;
            });
            return ServiceResult.Ok($"Studio \"{name}\" created.");
        }

        public async Task<ServiceResult> SetOwnerAsync(Guid id, string? ownerUserName)
        {
            var studio = GetById(id);
            if (studio == null) return ServiceResult.NotFound();

            var (owner, error) = await FindNewOwnerAsync(ownerUserName, studio.Id);
            if (error != null) return ServiceResult.Invalid(error);
            if (studio.UserId == owner?.Id) return ServiceResult.Ok("Nothing changed.");

            var previousOwnerId = studio.UserId;
            await _unitOfWork.InTransactionAsync(async () =>
            {
                studio.UserId = owner?.Id;
                _devRepository.Update(studio);

                await RevokeDevRoleAsync(previousOwnerId);
                if (owner != null) await GrantDevRoleAsync(owner);
                return true;
            });

            return ServiceResult.Ok(owner == null
                ? $"{studio.StudioName} no longer has a manager."
                : $"{owner.UserName} now manages {studio.StudioName}.");
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            var studio = GetById(id);
            if (studio == null) return ServiceResult.NotFound();

            // Games keep their studio; delist them instead
            if (_gameDevRepository.Any(gd => gd.DevId == id))
                return ServiceResult.Invalid("This studio still has games, so it can't be deleted.");

            var ownerId = studio.UserId;
            await _unitOfWork.InTransactionAsync(async () =>
            {
                _devRepository.Delete(studio);
                await RevokeDevRoleAsync(ownerId);
                return true;
            });
            return ServiceResult.Ok($"Studio \"{studio.StudioName}\" deleted.");
        }

        // Empty name = no owner. Each user manages at most one studio.
        private async Task<(GamingPlatformUser? Owner, string? Error)> FindNewOwnerAsync(string? userName, Guid? studioId)
        {
            if (string.IsNullOrWhiteSpace(userName)) return (null, null);

            var user = await _userManager.FindByNameAsync(userName.Trim());
            if (user == null) return (null, "There is no user with that username.");

            if (_devRepository.Any(d => d.UserId == user.Id && d.Id != studioId))
                return (null, $"{user.UserName} already manages another studio.");

            return (user, null);
        }

        private async Task GrantDevRoleAsync(GamingPlatformUser user)
        {
            if (!await _userManager.IsInRoleAsync(user, Roles.Dev))
                await _userManager.AddToRoleAsync(user, Roles.Dev);
        }

        private async Task RevokeDevRoleAsync(string? userId)
        {
            if (userId == null) return;
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && await _userManager.IsInRoleAsync(user, Roles.Dev))
                await _userManager.RemoveFromRoleAsync(user, Roles.Dev);
        }
    }
}
