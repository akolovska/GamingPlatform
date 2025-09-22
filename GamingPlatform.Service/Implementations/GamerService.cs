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
    public class GamerService : IGamerService
    {
        private readonly IRepository<Gamer> _gamerRepository;
        public void AddGamer(Gamer gamer)
        {
            _gamerRepository.Insert(gamer);
        }

        public void DeleteGamer(Guid gamerId)
        {
            Gamer gamer = GetGamerById(gamerId);
            _gamerRepository.Delete(gamer);
        }

        public void UpdateGamer(Gamer gamer)
        {
            _gamerRepository.Update(gamer);
        }

        public Gamer GetGamerById(Guid gamerId)
        {
            return _gamerRepository.Get(selector: g => g, 
                predicate: g => g.Id.Equals(gamerId));
        }

        public List<Gamer> GetAllGamers()
        {
            return _gamerRepository.GetAll(selector: g => g).ToList();
        }
    }
}
