using System;
using Domain.Enums;

namespace Application.DTOs.Complaints
{
    public class ComplaintHistoryDto
    {
        public Guid Id { get; set; }
        public ComplaintStatus FromStatus { get; set; }
        public ComplaintStatus ToStatus { get; set; }
        public Guid ChangedByUserId { get; set; }
        public DateTimeOffset ChangedAt { get; set; }
        public string? Remarks { get; set; }
    }
}
