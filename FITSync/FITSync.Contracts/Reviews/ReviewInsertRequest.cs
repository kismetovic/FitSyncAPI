using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Reviews;

public class ReviewInsertRequest
{
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int UserId { get; set; }
    public int TrainingId { get; set; }
}
