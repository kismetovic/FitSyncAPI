using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Reviews;

/// <summary>
/// Only the review's own content is editable. Reassigning a review to another user or
/// another training is not a legal operation, so those fields are absent.
/// </summary>
public class ReviewUpdateRequest
{
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string? Comment { get; set; }
}
