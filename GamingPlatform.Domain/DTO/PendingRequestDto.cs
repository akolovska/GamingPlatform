namespace GamingPlatform.Domain.DTO
{
    public class PendingRequestDto
    {
        public Guid Id { get; set; }              // the Friend row id
        public string SenderUsername { get; set; } = string.Empty;
    }
}
