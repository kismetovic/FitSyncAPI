using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Trainers;

public class TrainerAvailabilityRequest : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid trainer must be selected.")]
    public int TrainerId { get; set; }

    [EnumDataType(typeof(DayOfWeek), ErrorMessage = "Invalid day of week.")]
    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndTime <= StartTime)
            yield return new ValidationResult("End time must be after start time.", new[] { nameof(EndTime) });
        if (StartTime < TimeSpan.Zero || EndTime > TimeSpan.FromHours(24))
            yield return new ValidationResult("Times must fall within a single day.", new[] { nameof(StartTime) });
    }
}
