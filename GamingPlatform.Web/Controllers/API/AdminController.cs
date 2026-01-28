using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using GamingPlatform.API.Models;

namespace GamingPlatform.Web.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult FreeToGame()
        {
            var client = _httpClientFactory.CreateClient();
            var response = client.GetAsync("https://www.freetogame.com/api/games").Result;

            if (!response.IsSuccessStatusCode)
            {
                return View("Error", $"API request failed with status code {response.StatusCode}");
            }

            var json = response.Content.ReadAsStringAsync().Result;

            // Deserialize JSON into a list of objects
            var games = JsonSerializer.Deserialize<List<FreeToGameGame>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Explicitly point to the new folder view
            return View("~/Views/Admin/FreeToGames/Index.cshtml", games);
        }

        public IActionResult FreeToGameDetails(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var response = client.GetAsync($"https://www.freetogame.com/api/game?id={id}").Result;

            if (!response.IsSuccessStatusCode)
                return View("Error", $"API request failed with status code {response.StatusCode}");

            var json = response.Content.ReadAsStringAsync().Result;
            var game = JsonSerializer.Deserialize<FreeToGameGame>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return View("~/Views/Admin/FreeToGames/Details.cshtml", game);
        }
    }
}
