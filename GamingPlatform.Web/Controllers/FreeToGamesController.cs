using GamingPlatform.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GamingPlatform.Web.Controllers
{
    // Browse the free-to-play catalog from the public FreeToGame API
    [Authorize]
    public class FreeToGamesController : AppController
    {
        private readonly IFreeToGameService _freeToGameService;

        public FreeToGamesController(IFreeToGameService freeToGameService)
        {
            _freeToGameService = freeToGameService;
        }

        public async Task<IActionResult> Index()
        {
            var games = await _freeToGameService.GetGamesAsync();
            ViewBag.Unavailable = games == null;
            return View(games ?? new());
        }

        public async Task<IActionResult> Details(int id)
        {
            var game = await _freeToGameService.GetGameAsync(id);
            return game == null ? NotFound() : View(game);
        }
    }
}
