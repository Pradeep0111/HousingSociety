using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public required string FullName { get; set; }
        public Guid PrimaryUnitId { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
