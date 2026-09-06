using Domain.Enums;

namespace Application.DTOs.Complaints
{
    public class TransitionComplaintRequest
    {
        public ComplaintStatus ToStatus { get; set; }
        public string? Remarks { get; set; }
    }
}
