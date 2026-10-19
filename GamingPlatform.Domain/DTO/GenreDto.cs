namespace GamingPlatform.Domain.DTO
{
    public class GenreDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int GameCount { get; set; }
    }
}
