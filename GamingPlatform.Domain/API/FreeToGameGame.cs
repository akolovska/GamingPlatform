using System.Text.Json.Serialization;

namespace GamingPlatform.Domain.API
{
    // A game from the FreeToGame API (https://www.freetogame.com/api-doc).
    // The API uses snake_case names; FreeToGameService reads it with a snake_case policy.
    public class FreeToGameGame
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Thumbnail { get; set; }
        public string? ShortDescription { get; set; }
        public string? GameUrl { get; set; }
        public string? Genre { get; set; }
        public string? Platform { get; set; }
        public string? Publisher { get; set; }
        public string? Developer { get; set; }
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("freetogame_profile_url")]
        public string? FreeToGameProfileUrl { get; set; }
    }
}
