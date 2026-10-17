using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GamingPlatform.Domain.DTO
{
    public class GameFormDto
    {
        public Guid Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public List<Guid> GenreIds { get; set; } = new();
        public DateTime? ReleaseDate { get; set; }

        [Range(0, 10000)]
        public decimal? Price { get; set; }

        public string? Platform { get; set; }
        public string? CoverImageUrl { get; set; }
        public IFormFile? CoverFile { get; set; }
        public string? SystemRequirements { get; set; }

        public Guid? DevId { get; set; } // only used by admins on Create
    }
}
