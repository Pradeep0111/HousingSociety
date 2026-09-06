using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class DuesInvoice : BaseEntity
    {
        public required Guid MaintenanceCycleId { get; set; }
        public required Guid UnitId { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal LateFeeAmount { get; set; }
        public decimal TotalAmount => BaseAmount + LateFeeAmount;
        public DateOnly DueDate { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    }
}
