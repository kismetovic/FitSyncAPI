using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.TrainingTypes;

public class TrainingTypeUpdateRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 60 characters.")]
    public string Name { get; set; } = string.Empty;
}
