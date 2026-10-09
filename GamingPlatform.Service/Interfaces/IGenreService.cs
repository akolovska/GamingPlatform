using GamingPlatform.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Service.Interfaces
{
    public interface IGenreService
    {
        List<Genre> GetAll();
        Genre? GetById(Guid id);
        bool NameExists(string name, Guid? exceptId = null);
        bool IsInUse(Guid id);
        void Add(Genre genre);
        void Update(Genre genre);
        void Delete(Guid id);
    }
}
