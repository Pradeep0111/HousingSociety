using System;

namespace Application.DTOs.Complaints
{
    public class CreateComplaintRequest
    {
        public Guid UnitId { get; set; }
        public Guid ComplaintCategoryId { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
