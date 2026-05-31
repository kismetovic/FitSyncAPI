namespace FITSync.Contracts.Reviews;

public class ReviewUpdateRequest
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int UserId { get; set; }
    public int TrainingId { get; set; }
}
