using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class MaintenanceCycle : BaseEntity
    {
        public required Guid SocietyId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal RatePerSqFt { get; set; }
        public DateTimeOffset GeneratedAt { get; set; }
        public required Guid GeneratedByUserId { get; set; }
    }
}
