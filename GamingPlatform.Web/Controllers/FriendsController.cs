using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GamingPlatform.Web.Controllers
{
    [Authorize]
    public class FriendsController : Controller
    {
        private readonly IFriendService _friendService;
        private readonly UserManager<GamingPlatformUser> _userManager;

        public FriendsController(IFriendService friendService, UserManager<GamingPlatformUser> userManager)
        {
            _friendService = friendService;
            _userManager = userManager;
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptFriend(Guid requestId)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Verified"] = "Please verify your email to access all features.";
                return RedirectToAction("Index", "Account");
            }

            _friendService.AcceptFriendRequest(requestId, user.Id);

            TempData["FriendMessage"] = "Friend request accepted!";
            return RedirectToAction("Index", "Account");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFriend(Guid requestId)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Verified"] = "Please verify your email to access all features.";
                return RedirectToAction("Index", "Account");
            }

            bool success = _friendService.SendFriendRequest(user.Id, requestId);
            TempData["FriendMessage"] = success ? "Friend request sent!" : "Unable to send request.";

            return RedirectToAction("Index", "Account");
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFriend(Guid requestId)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Verified"] = "Please verify your email to access all features.";
                return RedirectToAction("Index", "Account");
            }
            _friendService.RemoveFriend(user.Id, requestId);
            TempData["FriendMessage"] = "Friend removed.";
            return RedirectToAction("Index", "Account");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineFriend(Guid requestId)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Verified"] = "Please verify your email to access all features.";
                return RedirectToAction("Index", "Account");
            }
            _friendService.RemoveFriend(user.Id, requestId);
            TempData["FriendMessage"] = "Friend request declined.";
            return RedirectToAction("Index", "Account");

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockFriend(Guid requestId)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                TempData["Verified"] = "Please verify your email to access all features.";
                return RedirectToAction("Index", "Account");
            }
            _friendService.BlockFriend(user.Id, requestId);
            TempData["FriendMessage"] = "Friend blocked.";
            return RedirectToAction("Index", "Account");
        }



        //GET: Friends/Details/5
        //public IActionResult Details(Guid requestId)
        //{
        //    var user = _userManager.FindByIdAsync(requestId.ToString()).Result;
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(user);
        //}

        //// GET: Friends/Create
        //public IActionResult Create()
        //{
        //    return View();
        //}

        //// POST: Friends/Create
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("UserId,FriendId,Status,CreatedAt,Id")] Friend friend)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        friend.Id = Guid.NewGuid();
        //        _context.Add(friend);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(friend);
        //}

        //// GET: Friends/Edit/5
        //public async Task<IActionResult> Edit(Guid? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var friend = await _context.Friends.FindAsync(id);
        //    if (friend == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(friend);
        //}

        //// POST: Friends/Edit/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(Guid id, [Bind("UserId,FriendId,Status,CreatedAt,Id")] Friend friend)
        //{
        //    if (id != friend.Id)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(friend);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!FriendExists(friend.Id))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(friend);
        //}

        //// GET: Friends/Delete/5
        //public async Task<IActionResult> Delete(Guid? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }

        //    var friend = await _context.Friends
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (friend == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(friend);
        //}

        //// POST: Friends/Delete/5
        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(Guid id)
        //{
        //    var friend = await _context.Friends.FindAsync(id);
        //    if (friend != null)
        //    {
        //        _context.Friends.Remove(friend);
        //    }

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(Index));
        //}

        //private bool FriendExists(Guid id)
        //{
        //    return _context.Friends.Any(e => e.Id == id);
        //}
    }
}
