using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.AdditionalServices;

public class AdditionalServiceInsertRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 80 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 10000, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }
}
