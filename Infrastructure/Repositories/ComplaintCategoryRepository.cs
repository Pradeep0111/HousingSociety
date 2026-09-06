using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ComplaintCategoryRepository : IComplaintCategoryRepository
    {
        private readonly AppDbContext _db;

        public ComplaintCategoryRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ComplaintCategory?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.ComplaintCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
