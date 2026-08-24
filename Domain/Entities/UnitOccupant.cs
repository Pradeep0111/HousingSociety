using Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class UnitOccupant : BaseEntity
    {
        public required Guid UnitId { get; set; }
        public required Guid ApplicationUserId { get; set; }
        public OccupantType Type { get; set; }
        public DateTimeOffset MoveInDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
