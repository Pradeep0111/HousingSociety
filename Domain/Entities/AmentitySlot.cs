using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class AmentitySlot : BaseEntity
    {
        public required string AmentityName { get; set; }
        public DateTimeOffset SlotStart { get; set; }
        public DateTimeOffset SlotEnd { get; set; }
        public Guid? BookedByUserId { get; set; }
        public DateTimeOffset? BookedAt { get; set; }
    }
}
