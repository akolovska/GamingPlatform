using GamingPlatform.API.Models;
using GamingPlatform.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;

namespace GamingPlatform.Web.Controllers
{
    public class FreeToGamesController : Controller
    {
        private readonly HttpClient _httpClient;

        public FreeToGamesController()
        {
            _httpClient = new HttpClient();
        }

        public async Task<IActionResult> Index()
        {
            string URL = "https://www.freetogame.com/api/games";
            var games = await _httpClient.GetFromJsonAsync<List<FreeToGameGame>>(URL);
            return View("~/Views/FreeToGames/Index.cshtml", games);
        }

        public async Task<IActionResult> Details(int id)
        {
            string URL = $"https://www.freetogame.com/api/game?id={id}";
            var game = await _httpClient.GetFromJsonAsync<FreeToGameGame>(URL);
            return View("~/Views/FreeToGames/Details.cshtml", game);
        }

    }
}
