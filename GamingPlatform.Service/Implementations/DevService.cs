using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Interface;
using GamingPlatform.Service.Interfaces;

namespace GamingPlatform.Service.Implementations
{
    public class DevService : IDevService
    {
        private readonly IRepository<Dev> _devRepository;

        public DevService(IRepository<Dev> devRepository)
        {
            _devRepository = devRepository;
        }

        public void AddDev(Dev dev) => _devRepository.Insert(dev);

        public void UpdateDev(Dev dev) => _devRepository.Update(dev);

        public void DeleteDev(Guid id)
        {
            var dev = GetDevById(id);
            if (dev != null)
                _devRepository.Delete(dev);
        }

        public Dev? GetDevById(Guid id) =>
            _devRepository.Get(selector: d => d, predicate: d => d.Id == id);

        public Dev? GetDevByUserId(string userId) =>
            _devRepository.Get(selector: d => d, predicate: d => d.UserId == userId);

        public List<Dev> GetAllDevs() =>
            _devRepository.GetAll(selector: d => d)
                .OrderBy(d => d.StudioName)
                .ToList();
    }
}