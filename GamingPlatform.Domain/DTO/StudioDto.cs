namespace GamingPlatform.Domain.DTO
{
    // A studio as the admin page lists it
    public class StudioDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OwnerUserName { get; set; }
        public int GameCount { get; set; }
    }
}
