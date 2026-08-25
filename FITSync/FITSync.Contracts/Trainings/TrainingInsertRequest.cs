using System.ComponentModel.DataAnnotations;
using FITSync.Domain.Enums;

namespace FITSync.Contracts.Trainings;

public class TrainingInsertRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters.")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    [Range(5, 600, ErrorMessage = "Duration must be between 5 and 600 minutes.")]
    public int DurationMinutes { get; set; }

    [Range(1, 500, ErrorMessage = "Capacity must be at least 1.")]
    public int MaxCapacity { get; set; }

    [EnumDataType(typeof(TrainingDifficulty), ErrorMessage = "Invalid difficulty.")]
    public TrainingDifficulty Difficulty { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A valid training type must be selected.")]
    public int TrainingTypeId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Invalid trainer.")]
    public int? TrainerId { get; set; }
}
