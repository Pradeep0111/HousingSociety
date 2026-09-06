using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces
{
    public interface IComplaintRepository
    {
        void Add(Complaint complaint);

        Task<Complaint?> GetByIdAsync(Guid id, CancellationToken ct = default);

        Task<(IReadOnlyList<Complaint> Items, int TotalCount)> ListAsync(
            Guid? unitId, ComplaintStatus? status, int page, int pageSize, CancellationToken ct = default);
    }
}
