using System;
using Domain.Enums;

namespace Application.DTOs.Complaints
{
    // Plain settable-property class (not a record) so ASP.NET Core's [FromQuery]
    // complex-object binder resolves it reliably from query-string parameters.
    public class ComplaintListQuery
    {
        public ComplaintStatus? Status { get; set; }
        public Guid? UnitId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
