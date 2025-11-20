using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GamingPlatform.Domain.Identity;

namespace GamingPlatform.Repository.Interface
{
    interface IUserRepository
    {
        GamingPlatformUser GetUserById(string id);
    }
}
