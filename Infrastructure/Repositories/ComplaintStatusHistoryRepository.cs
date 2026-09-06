using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ComplaintStatusHistoryRepository : IComplaintStatusHistoryRepository
    {
        private readonly AppDbContext _db;

        public ComplaintStatusHistoryRepository(AppDbContext db)
        {
            _db = db;
        }

        public void Add(ComplaintStatusHistory history) => _db.ComplaintStatusHistories.Add(history);

        public async Task<IReadOnlyList<ComplaintStatusHistory>> GetByComplaintIdAsync(Guid complaintId, CancellationToken ct = default) =>
            await _db.ComplaintStatusHistories
                .Where(h => h.ComplaintId == complaintId)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync(ct);
    }
}
