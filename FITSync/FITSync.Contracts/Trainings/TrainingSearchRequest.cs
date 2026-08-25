using System.ComponentModel.DataAnnotations;
using FITSync.Contracts.Common;
using FITSync.Domain.Enums;

namespace FITSync.Contracts.Trainings;

public class TrainingSearchRequest : PagedRequest
{
    [StringLength(100)]
    public string? Name { get; set; }

    [Range(0, 100000, ErrorMessage = "MinPrice cannot be negative.")]
    public decimal? MinPrice { get; set; }

    [Range(0, 100000, ErrorMessage = "MaxPrice cannot be negative.")]
    public decimal? MaxPrice { get; set; }

    public int? TrainingTypeId { get; set; }
    public int? TrainerId { get; set; }

    [EnumDataType(typeof(TrainingDifficulty), ErrorMessage = "Invalid difficulty.")]
    public TrainingDifficulty? Difficulty { get; set; }
}
