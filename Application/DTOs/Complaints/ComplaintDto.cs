using System;
using Domain.Enums;

namespace Application.DTOs.Complaints
{
    public class ComplaintDto
    {
        public Guid Id { get; set; }
        public Guid UnitId { get; set; }
        public Guid RaisedByUserId { get; set; }
        public Guid ComplaintCategoryId { get; set; }
        public string Description { get; set; } = string.Empty;
        public ComplaintStatus Status { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset SlaDeadline { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
    }
}
