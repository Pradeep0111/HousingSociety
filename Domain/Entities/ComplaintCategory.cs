using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class ComplaintCategory : BaseEntity
    {
        public required string Name { get; set; }
        public int SlaHours { get; set; }
    }
}
