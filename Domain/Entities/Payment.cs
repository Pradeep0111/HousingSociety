using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Payment : BaseEntity
    {
        public required Guid DuesInvoiceId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTimeOffset PaymentDate { get; set; }
        public required string ReferenceNumber { get; set; }
        public required Guid RecordedByUserId { get; set; }
    }
}
