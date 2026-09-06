using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly AppDbContext _db;

        public ComplaintRepository(AppDbContext db)
        {
            _db = db;
        }

        public void Add(Complaint complaint) => _db.Complaints.Add(complaint);

        public async Task<Complaint?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            await _db.Complaints.FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<(IReadOnlyList<Complaint> Items, int TotalCount)> ListAsync(
            Guid? unitId, ComplaintStatus? status, int page, int pageSize, CancellationToken ct = default)
        {
            var complaints = _db.Complaints.AsQueryable();

            if (unitId is not null)
                complaints = complaints.Where(c => c.UnitId == unitId);

            if (status is not null)
                complaints = complaints.Where(c => c.Status == status);

            complaints = complaints.OrderByDescending(c => c.CreatedAt);

            var totalCount = await complaints.CountAsync(ct);
            var items = await complaints
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }
    }
}
