using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.StateMachines
{
    public static class ComplaintStateMachine
    {
        private static readonly Dictionary<ComplaintStatus, ComplaintStatus[]> AllowedTransitions = new Dictionary<ComplaintStatus, ComplaintStatus[]>
        {
            { ComplaintStatus.Open, new[] { ComplaintStatus.Assigned, ComplaintStatus.Escalated } },
            { ComplaintStatus.Assigned, new[] { ComplaintStatus.InProgress, ComplaintStatus.Escalated } },
            { ComplaintStatus.InProgress, new[] { ComplaintStatus.Resolved } },
            { ComplaintStatus.Escalated, new[] { ComplaintStatus.Assigned } },
            { ComplaintStatus.Resolved, new[] { ComplaintStatus.Reopened } },
            { ComplaintStatus.Reopened, new[] { ComplaintStatus.Assigned } },
        };
        public static bool CanTransition(ComplaintStatus from, ComplaintStatus to) =>
                AllowedTransitions.TryGetValue(from, out var next) && next.Contains(to);
    }
}
