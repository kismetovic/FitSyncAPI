using FITSync.Domain.Models;

namespace FITSync.Domain.Entities
{
    /// <summary>
    /// A question and answer shown on the mobile "Pomoć i podrška" screen.
    /// The list used to be a hardcoded array inside the Flutter widget, so it could
    /// only change by shipping a new build; it is now content the administrator owns.
    /// </summary>
    public class Faq : BaseEntity
    {
        public string Question { get; set; } = string.Empty;

        public string Answer { get; set; } = string.Empty;

        /// <summary>Ascending display order. Ties fall back to Id.</summary>
        public int SortOrder { get; set; }

        /// <summary>Lets an administrator retire an entry without losing the text.</summary>
        public bool IsActive { get; set; } = true;
    }
}
