using GamingPlatform.Domain.Identity;
using GamingPlatform.Repository.Data;
using GamingPlatform.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Repository.Implementation
{
    class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<GamingPlatformUser> entites;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
            this.entites = _context.Set<GamingPlatformUser>();
        }

        public GamingPlatformUser GetUserById(string id)
        {
            return entites.First(ent => ent.Id == id);
        }
    }
}
