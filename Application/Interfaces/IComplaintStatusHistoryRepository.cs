using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IComplaintStatusHistoryRepository
    {
        void Add(ComplaintStatusHistory history);

        Task<IReadOnlyList<ComplaintStatusHistory>> GetByComplaintIdAsync(Guid complaintId, CancellationToken ct = default);
    }
}
