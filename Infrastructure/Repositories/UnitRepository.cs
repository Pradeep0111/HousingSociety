using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly AppDbContext _db;

        public UnitRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
            await _db.Units.AnyAsync(u => u.Id == id, ct);

        public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Units.FirstOrDefaultAsync(u => u.Id == id, ct);
    }
}
