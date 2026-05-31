using FITSync.Domain.Enums;

namespace FITSync.Contracts.Trainings;

public class TrainingSearchRequest
{
    public string? Name { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? TrainingTypeId { get; set; }
    public TrainingDifficulty? Difficulty { get; set; }
}
