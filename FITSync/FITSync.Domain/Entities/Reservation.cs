using FITSync.Domain.Enums;
using FITSync.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Domain.Entities
{
    public class Reservation : BaseEntity
    {
        public DateTime ReservationDate { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Initial;
        public ReservationType ReservationType { get; set; } = ReservationType.OneTime;
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public int TrainingId { get; set; }
        public virtual Training Training { get; set; } = null!;
        public virtual ICollection<ReservationService> ReservationServices { get; set; } = new List<ReservationService>();
        public virtual Payment? Payment { get; set; }
    }
}
