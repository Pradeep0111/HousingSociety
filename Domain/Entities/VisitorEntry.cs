using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class VisitorEntry : BaseEntity
    {
        public Guid UnitId { get; set; }
        public required string VisitorName { get; set; }
        public required string Purpose { get; set; }
        public required Guid LoggedbyGuardUserId { get; set; }
        public DateTimeOffset EntryTime { get; set; }
        public DateTimeOffset ExitTime { get; set; }
    }
}
