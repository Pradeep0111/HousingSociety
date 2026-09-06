using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IUnitRepository
    {
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

        Task<Unit?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }
}
