using System.ComponentModel.DataAnnotations;

namespace GamingPlatform.Domain.DTO
{
    public class DeleteAccountDto
    {
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
