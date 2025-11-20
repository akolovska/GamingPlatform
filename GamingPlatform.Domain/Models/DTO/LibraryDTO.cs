using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.Models.DTO
{
    public class LibraryDTO : BaseEntity
    {
        public Guid? GamerId { get; set; }
        public string? GameName { get; set; }
        public DateTime? DateAdded { get; set; }
        public int PlayTimeHours { get; set; }
    }
}
