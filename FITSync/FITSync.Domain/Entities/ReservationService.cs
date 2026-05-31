using FITSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Domain.Entities
{
    public class ReservationService : BaseEntity
    {
        public int ReservationId { get; set; }
        public virtual Reservation Reservation { get; set; } = null!;
        public int AdditionalServiceId { get; set; }
        public virtual AdditionalService AdditionalService { get; set; } = null!;
    }
}
