using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    /// <summary>
    /// One weekly window in which a trainer normally works. A reservation whose whole
    /// duration does not fit inside some window is an "outside availability" request:
    /// it needs trainer approval and carries a surcharge.
    /// </summary>
    public class TrainerAvailability : BaseEntity
    {
        public int TrainerId { get; set; }
        public virtual Trainer Trainer { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>Local start of the window, e.g. 08:00.</summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>Local end of the window, e.g. 16:00.</summary>
        public TimeSpan EndTime { get; set; }

        public bool Covers(DateTime start, DateTime end)
        {
            if (start.DayOfWeek != DayOfWeek) return false;
            if (end.Date != start.Date) return false;
            return start.TimeOfDay >= StartTime && end.TimeOfDay <= EndTime;
        }
    }
}
