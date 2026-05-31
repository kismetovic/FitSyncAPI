using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FITSync.Domain.Enums
{
    public enum ReservationStatus
    {
        Initial = 0,
        Approved = 1,
        Paid = 2,
        Cancelled = 3,
        Completed = 4,
        PendingApproval = 5
    }
}
