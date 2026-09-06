using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.DTOs.Complaints;

namespace Application.Interfaces
{
    public interface IComplaintService
    {
        Task<ComplaintDto> CreateAsync(CreateComplaintRequest request, ClaimsPrincipal actingUser, CancellationToken ct = default);

        Task<ComplaintDto> GetByIdAsync(Guid id, ClaimsPrincipal actingUser, CancellationToken ct = default);

        Task<PagedResult<ComplaintDto>> ListAsync(ComplaintListQuery query, ClaimsPrincipal actingUser, CancellationToken ct = default);

        Task<ComplaintDto> TransitionAsync(Guid id, TransitionComplaintRequest request, ClaimsPrincipal actingUser, CancellationToken ct = default);

        Task<ComplaintDto> AssignAsync(Guid id, AssignComplaintRequest request, ClaimsPrincipal actingUser, CancellationToken ct = default);

        Task<IReadOnlyList<ComplaintHistoryDto>> GetHistoryAsync(Guid id, ClaimsPrincipal actingUser, CancellationToken ct = default);
    }
}
