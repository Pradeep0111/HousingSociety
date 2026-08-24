using Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Complaint : BaseEntity
    {
        public Guid UnitId { get; set; }
        public Guid RaisedByUserId { get; set; }
        public Guid ComplaintCategoryId { get; set; }
        public required string Description { get; set; }
        public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;
        public Guid? AssignedToUserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset SlaDeadline { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
    }
}
