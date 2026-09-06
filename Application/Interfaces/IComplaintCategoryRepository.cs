using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IComplaintCategoryRepository
    {
        Task<ComplaintCategory?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
