using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enum
{
    public enum ComplaintStatus
    {
        Open,
        Assigned,
        InProgress,
        Escalated,
        Resolved,
        Reopened
    }
}
