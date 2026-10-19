using System.Net.Http.Json;
using System.Text.Json;
using GamingPlatform.Domain.API;
using GamingPlatform.Service.Interfaces;
using Microsoft.Extensions.Logging;

namespace GamingPlatform.Service.Implementations
{
    // HttpClient comes from IHttpClientFactory (registered with AddHttpClient in Program.cs),
    // which reuses connections instead of opening new sockets per request
    public class FreeToGameService : IFreeToGameService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower // the API sends short_description, game_url, ...
        };

        private readonly HttpClient _http;
        private readonly ILogger<FreeToGameService> _logger;

        public FreeToGameService(HttpClient http, ILogger<FreeToGameService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public Task<List<FreeToGameGame>?> GetGamesAsync() => GetAsync<List<FreeToGameGame>>("games");

        public async Task<FreeToGameGame?> GetGameAsync(int id)
        {
            var game = await GetAsync<FreeToGameGame>($"game?id={id}");
            return game?.Id > 0 ? game : null; // an unknown id comes back as an object with no id
        }

        private async Task<T?> GetAsync<T>(string path) where T : class
        {
            try
            {
                return await _http.GetFromJsonAsync<T>(path, JsonOptions);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _logger.LogWarning(ex, "FreeToGame request {Path} failed", path);
                return null;
            }
        }
    }
}
