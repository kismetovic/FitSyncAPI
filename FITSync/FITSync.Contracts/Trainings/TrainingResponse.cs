using FITSync.Domain.Enums;

namespace FITSync.Contracts.Trainings;

public class TrainingResponse
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public int MaxCapacity { get; set; }
    public TrainingDifficulty Difficulty { get; set; }
    public int TrainingTypeId { get; set; }
    public TrainingTypeSummaryResponse? TrainingType { get; set; }
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class TrainingTypeSummaryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
