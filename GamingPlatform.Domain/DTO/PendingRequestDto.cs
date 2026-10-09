using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamingPlatform.Domain.DTO
{
    public class PendingRequestDto
    {
        public Guid Id { get; set; }              // the Friend row id
        public string SenderUsername { get; set; } = string.Empty;
    }
}