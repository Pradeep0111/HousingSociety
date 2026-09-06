using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    }
}
