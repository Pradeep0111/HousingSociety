using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common;
using Application.DTOs.Complaints;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/complaints")]
    public class ComplaintsController : ControllerBase
    {
        private readonly IComplaintService _complaintService;

        public ComplaintsController(IComplaintService complaintService)
        {
            _complaintService = complaintService;
        }

        [HttpPost]
        public async Task<ActionResult<ComplaintDto>> Create([FromBody] CreateComplaintRequest request, CancellationToken ct)
        {
            var complaint = await _complaintService.CreateAsync(request, User, ct);
            return CreatedAtAction(nameof(GetById), new { id = complaint.Id }, complaint);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ComplaintDto>> GetById(Guid id, CancellationToken ct)
        {
            var complaint = await _complaintService.GetByIdAsync(id, User, ct);
            return Ok(complaint);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<ComplaintDto>>> List([FromQuery] ComplaintListQuery query, CancellationToken ct)
        {
            var result = await _complaintService.ListAsync(query, User, ct);
            return Ok(result);
        }

        [HttpPost("{id:guid}/transition")]
        public async Task<ActionResult<ComplaintDto>> Transition(Guid id, [FromBody] TransitionComplaintRequest request, CancellationToken ct)
        {
            var complaint = await _complaintService.TransitionAsync(id, request, User, ct);
            return Ok(complaint);
        }

        [HttpPost("{id:guid}/assign")]
        public async Task<ActionResult<ComplaintDto>> Assign(Guid id, [FromBody] AssignComplaintRequest request, CancellationToken ct)
        {
            var complaint = await _complaintService.AssignAsync(id, request, User, ct);
            return Ok(complaint);
        }

        [HttpGet("{id:guid}/history")]
        public async Task<ActionResult<IReadOnlyList<ComplaintHistoryDto>>> History(Guid id, CancellationToken ct)
        {
            var history = await _complaintService.GetHistoryAsync(id, User, ct);
            return Ok(history);
        }
    }
}
