using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    /// <summary>
    /// The gym's own support details, shown on the mobile help screen.
    /// A single row (Id 1): there is one gym, and the administrator edits it in the
    /// desktop app rather than it being invented in Flutter source, which is where
    /// the placeholder "support@fitsync.app" and a US phone number came from.
    /// </summary>
    public class SupportContact : BaseEntity
    {
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>Free text, e.g. "Pon – Pet, 08:00 – 20:00".</summary>
        public string WorkingHours { get; set; } = string.Empty;

        public string? Address { get; set; }
    }
}
