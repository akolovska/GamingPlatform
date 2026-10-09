using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces
{
    public interface IDevService
    {
        void AddDev(Dev dev);
        void UpdateDev(Dev dev);
        void DeleteDev(Guid id);
        Dev? GetDevById(Guid id);
        Dev? GetDevByUserId(string userId);
        List<Dev> GetAllDevs();
    }
}
