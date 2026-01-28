using GamingPlatform.Domain.Enum;
using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Service.Interfaces
{
    public interface IFriendService
    {
        bool SendFriendRequest(string userId, string friendUsername);
        List<GamingPlatformUser> GetFriends(string userId);
        void AcceptFriendRequest(Guid friendRequestId, string userId);
        List<Friend> GetPendingRequests(string id);
    }
}
