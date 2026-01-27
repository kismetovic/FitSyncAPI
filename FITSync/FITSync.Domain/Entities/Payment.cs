using FITSync.Domain.Enums;
using FITSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Domain.Entities
{
    public class Payment : BaseEntity
    {
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Currency { get; set; } = "BAM";
        public PaymentProvider PaymentProvider { get; set; }
        public int ReservationId { get; set; }
        public virtual Reservation Reservation { get; set; } = null!;
    }
}
