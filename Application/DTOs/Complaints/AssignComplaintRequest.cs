using System;

namespace Application.DTOs.Complaints
{
    public class AssignComplaintRequest
    {
        public Guid AssignedToUserId { get; set; }
        public string? Remarks { get; set; }
    }
}
