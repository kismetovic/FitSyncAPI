using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Reviews;

/// <summary>
/// A review is always written by the caller (UserId comes from the JWT) and is always
/// tied to one of the caller's own completed reservations.
/// </summary>
public class ReviewInsertRequest
{
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string? Comment { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A valid reservation must be selected.")]
    public int ReservationId { get; set; }
}
