using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public void AddDev(Dev dev)
        {
            _devRepository.Insert(dev);
        }

        public void UpdateDev(Dev dev)
        {
            _devRepository.Update(dev);
        }

        public void DeleteDev(Guid id)
        {
            Dev dev = GetDevById(id);
            _devRepository.Delete(dev);
        }

        public Dev GetDevById(Guid id)
        {
            return _devRepository.Get(selector: x => x,
                predicate: x => x.Id.Equals(id));
        }

        public List<Dev> GetAllDevs()
        {
            return _devRepository.GetAll(selector: x => x).ToList();
        }
    }
}
