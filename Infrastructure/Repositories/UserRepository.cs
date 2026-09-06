using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
            await _db.Users.AnyAsync(u => u.Id == id, ct);
    }
}
