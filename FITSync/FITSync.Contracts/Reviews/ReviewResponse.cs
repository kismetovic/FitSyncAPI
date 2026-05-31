namespace FITSync.Contracts.Reviews;

public class ReviewResponse
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public int UserId { get; set; }
    public int TrainingId { get; set; }
    public string? UserName { get; set; }
    public string? TrainingName { get; set; }
}
