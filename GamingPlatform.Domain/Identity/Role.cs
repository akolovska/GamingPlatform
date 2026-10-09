using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Identity
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Dev = "Dev";

        // Convenience for "devs and admins"
        public const string DevOrAdmin = Admin + "," + Dev;
    }
}
