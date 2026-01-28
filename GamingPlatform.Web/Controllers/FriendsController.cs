using GamingPlatform.Domain.Identity;
using GamingPlatform.Domain.Models;
using GamingPlatform.Repository.Data;
using GamingPlatform.Service.Implementations;
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
    public class FriendsController : Controller
    {
        private readonly FriendService _friendService;
        private readonly UserManager<GamingPlatformUser> _userManager;

        public FriendsController(FriendService friendService, UserManager<GamingPlatformUser> userManager)
        {
            _friendService = friendService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var userId = _userManager.GetUserId(User);
            var friends = _friendService.GetFriends(userId);
            return View(friends);
        }
        [HttpPost]
        public IActionResult AddFriend(string username)
        {
            var userId = _userManager.GetUserId(User);
            var success = _friendService.SendFriendRequest(userId, username);

            TempData["Message"] = success ? "Friend request sent!" : "Cannot send request.";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptFriend(Guid requestId)
        {
            var user = _userManager.GetUserAsync(User).Result;
            if (user == null) return NotFound();

            _friendService.AcceptFriendRequest(requestId, user.Id);

            TempData["FriendMessage"] = "Friend request accepted!";
            return RedirectToAction(nameof(Index));
        }


        // GET: Friends/Details/5
        //public async Task<IActionResult> Details(Guid? id)
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
