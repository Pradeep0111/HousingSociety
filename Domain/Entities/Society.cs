using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Society : BaseEntity
    {
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string City { get; set; }
        public required string Pincode { get; set; }
        public decimal DefaultRatePerSqFt { get; set; }
    }
}
