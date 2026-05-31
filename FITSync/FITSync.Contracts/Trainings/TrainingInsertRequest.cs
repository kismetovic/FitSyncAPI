using FITSync.Domain.Enums;

namespace FITSync.Contracts.Trainings;

public class TrainingInsertRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public int MaxCapacity { get; set; }
    public TrainingDifficulty Difficulty { get; set; }
    public int TrainingTypeId { get; set; }
}
