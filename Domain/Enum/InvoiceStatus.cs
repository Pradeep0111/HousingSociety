using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Enum
{
    public enum InvoiceStatus
    {
        Pending,
        PartiallyPaid,
        Paid,
        Overdue
    }
}
