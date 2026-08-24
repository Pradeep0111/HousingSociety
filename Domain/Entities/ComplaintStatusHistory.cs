using Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class ComplaintStatusHistory : BaseEntity
    {
        public required Guid ComplaintId { get; set; }
        public ComplaintStatus FromStatus { get; set; }
        public ComplaintStatus ToStatus { get; set; }
        public required Guid ChangedByUserId { get; set; }
        public DateTimeOffset ChangedAt { get; set; }
        public string? Remarks { get; set; }
    }
}
