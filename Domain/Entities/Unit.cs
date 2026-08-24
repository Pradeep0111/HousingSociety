using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Unit : BaseEntity
    {
        public required Guid SocietyId { get; set; }
        public required string BlockName { get; set; }
        public required string UnitNumber { get; set; }
        public int FloorNumber { get; set; }
        public decimal AreaSqFt { get; set; }
        public required Guid OwnerId { get; set; } 
    }
}
