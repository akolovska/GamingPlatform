using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamingPlatform.Domain.Models;

namespace GamingPlatform.Service.Interfaces
{
    public interface IGamerService
    {
        void AddGamer(Gamer gamer);
        void DeleteGamer(Guid gamerId);
        void UpdateGamer(Gamer gamer);
        Gamer GetGamerById(Guid gamerId);
        List<Gamer> GetAllGamers();
    }
}
