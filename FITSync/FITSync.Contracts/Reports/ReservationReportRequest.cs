using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Reports;

public class ReservationReportRequest : IValidatableObject
{
    [Required(ErrorMessage = "From date is required.")]
    public DateTime From { get; set; }

    [Required(ErrorMessage = "To date is required.")]
    public DateTime To { get; set; }

    public int? TrainingId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (To < From)
            yield return new ValidationResult("To date must be on or after From date.", new[] { nameof(To) });
        if ((To - From).TotalDays > 366)
            yield return new ValidationResult("The reporting period cannot exceed 366 days.", new[] { nameof(To) });
    }
}
