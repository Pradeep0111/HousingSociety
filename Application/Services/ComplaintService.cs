using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.DTOs.Complaints;
using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.StateMachines;

namespace Application.Services
{
    public class ComplaintService : IComplaintService
    {
        private readonly IComplaintRepository _complaintRepository;
        private readonly IComplaintCategoryRepository _categoryRepository;
        private readonly IComplaintStatusHistoryRepository _historyRepository;
        private readonly IUnitAccessGuard _unitAccessGuard;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ComplaintService(
            IComplaintRepository complaintRepository,
            IComplaintCategoryRepository categoryRepository,
            IComplaintStatusHistoryRepository historyRepository,
            IUnitAccessGuard unitAccessGuard,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _complaintRepository = complaintRepository;
            _categoryRepository = categoryRepository;
            _historyRepository = historyRepository;
            _unitAccessGuard = unitAccessGuard;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ComplaintDto> CreateAsync(CreateComplaintRequest request, ClaimsPrincipal actingUser, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Description))
                throw new ValidationException("Description is required.");

            await _unitAccessGuard.EnsureUnitAccessAsync(actingUser, request.UnitId, ct);

            var category = await _categoryRepository.GetByIdAsync(request.ComplaintCategoryId, ct)
                ?? throw new NotFoundException($"Complaint category '{request.ComplaintCategoryId}' was not found.");

            var now = DateTimeOffset.UtcNow;
            var complaint = new Complaint
            {
                Id = Guid.NewGuid(),
                UnitId = request.UnitId,
                RaisedByUserId = GetUserId(actingUser),
                ComplaintCategoryId = request.ComplaintCategoryId,
                Description = request.Description,
                Status = ComplaintStatus.Open,
                CreatedAt = now,
                SlaDeadline = now.AddHours(category.SlaHours),
            };

            _complaintRepository.Add(complaint);
            // No prior state exists yet, so the creation event records From == To == Open.
            _historyRepository.Add(new ComplaintStatusHistory
            {
                Id = Guid.NewGuid(),
                ComplaintId = complaint.Id,
                FromStatus = ComplaintStatus.Open,
                ToStatus = ComplaintStatus.Open,
                ChangedByUserId = complaint.RaisedByUserId,
                ChangedAt = now,
                Remarks = "Complaint raised.",
            });

            await _unitOfWork.SaveChangesAsync(ct);

            return ToDto(complaint);
        }

        public async Task<ComplaintDto> GetByIdAsync(Guid id, ClaimsPrincipal actingUser, CancellationToken ct = default)
        {
            var complaint = await GetComplaintOrThrowAsync(id, ct);
            await _unitAccessGuard.EnsureUnitAccessAsync(actingUser, complaint.UnitId, ct);

            return ToDto(complaint);
        }

        public async Task<PagedResult<ComplaintDto>> ListAsync(ComplaintListQuery query, ClaimsPrincipal actingUser, CancellationToken ct = default)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize switch
            {
                < 1 => 20,
                > 200 => 200,
                _ => query.PageSize,
            };

            Guid? unitFilter;
            if (IsAdmin(actingUser))
            {
                unitFilter = query.UnitId;
            }
            else
            {
                // Residents only ever see their own unit's complaints, regardless of
                // what unitId (if any) was requested.
                unitFilter = GetOwnUnitId(actingUser)
                    ?? throw new ForbiddenAccessException("No unit is associated with the current user.");
            }

            var (items, totalCount) = await _complaintRepository.ListAsync(unitFilter, query.Status, page, pageSize, ct);

            return new PagedResult<ComplaintDto>
            {
                Items = items.Select(ToDto).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<ComplaintDto> TransitionAsync(Guid id, TransitionComplaintRequest request, ClaimsPrincipal actingUser, CancellationToken ct = default)
        {
            var complaint = await GetComplaintOrThrowAsync(id, ct);
            await _unitAccessGuard.EnsureUnitAccessAsync(actingUser, complaint.UnitId, ct);

            if (!ComplaintStateMachine.CanTransition(complaint.Status, request.ToStatus))
                throw new InvalidTransitionException($"Cannot transition complaint from '{complaint.Status}' to '{request.ToStatus}'.");

            ApplyTransition(complaint, request.ToStatus, GetUserId(actingUser), request.Remarks);

            await _unitOfWork.SaveChangesAsync(ct);

            return ToDto(complaint);
        }

        public async Task<ComplaintDto> AssignAsync(Guid id, AssignComplaintRequest request, ClaimsPrincipal actingUser, CancellationToken ct = default)
        {
            var complaint = await GetComplaintOrThrowAsync(id, ct);
            await _unitAccessGuard.EnsureUnitAccessAsync(actingUser, complaint.UnitId, ct);

            if (!ComplaintStateMachine.CanTransition(complaint.Status, ComplaintStatus.Assigned))
                throw new InvalidTransitionException($"Cannot assign a complaint that is currently '{complaint.Status}'.");

            var assigneeExists = await _userRepository.ExistsAsync(request.AssignedToUserId, ct);
            if (!assigneeExists)
                throw new NotFoundException($"User '{request.AssignedToUserId}' was not found.");

            complaint.AssignedToUserId = request.AssignedToUserId;
            ApplyTransition(complaint, ComplaintStatus.Assigned, GetUserId(actingUser), request.Remarks);

            await _unitOfWork.SaveChangesAsync(ct);

            return ToDto(complaint);
        }

        public async Task<IReadOnlyList<ComplaintHistoryDto>> GetHistoryAsync(Guid id, ClaimsPrincipal actingUser, CancellationToken ct = default)
        {
            var complaint = await GetComplaintOrThrowAsync(id, ct);
            await _unitAccessGuard.EnsureUnitAccessAsync(actingUser, complaint.UnitId, ct);

            var history = await _historyRepository.GetByComplaintIdAsync(id, ct);
            return history.Select(ToHistoryDto).ToList();
        }

        private void ApplyTransition(Complaint complaint, ComplaintStatus toStatus, Guid changedByUserId, string? remarks)
        {
            var now = DateTimeOffset.UtcNow;
            var fromStatus = complaint.Status;

            complaint.Status = toStatus;
            complaint.ResolvedAt = toStatus == ComplaintStatus.Resolved ? now : null;

            _historyRepository.Add(new ComplaintStatusHistory
            {
                Id = Guid.NewGuid(),
                ComplaintId = complaint.Id,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ChangedByUserId = changedByUserId,
                ChangedAt = now,
                Remarks = remarks,
            });
        }

        private async Task<Complaint> GetComplaintOrThrowAsync(Guid id, CancellationToken ct) =>
            await _complaintRepository.GetByIdAsync(id, ct)
                ?? throw new NotFoundException($"Complaint '{id}' was not found.");

        private static bool IsAdmin(ClaimsPrincipal user) =>
            user.IsInRole("Secretary") || user.IsInRole("CommitteeMember");

        private static Guid? GetOwnUnitId(ClaimsPrincipal user) =>
            Guid.TryParse(user.FindFirstValue("unitId"), out var unitId) ? unitId : null;

        private static Guid GetUserId(ClaimsPrincipal user) =>
            Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? userId
                : throw new ForbiddenAccessException("The current user could not be identified.");

        private static ComplaintDto ToDto(Complaint c) => new()
        {
            Id = c.Id,
            UnitId = c.UnitId,
            RaisedByUserId = c.RaisedByUserId,
            ComplaintCategoryId = c.ComplaintCategoryId,
            Description = c.Description,
            Status = c.Status,
            AssignedToUserId = c.AssignedToUserId,
            CreatedAt = c.CreatedAt,
            SlaDeadline = c.SlaDeadline,
            ResolvedAt = c.ResolvedAt,
        };

        private static ComplaintHistoryDto ToHistoryDto(ComplaintStatusHistory h) => new()
        {
            Id = h.Id,
            FromStatus = h.FromStatus,
            ToStatus = h.ToStatus,
            ChangedByUserId = h.ChangedByUserId,
            ChangedAt = h.ChangedAt,
            Remarks = h.Remarks,
        };
    }
}
