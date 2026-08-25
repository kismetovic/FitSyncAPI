using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Faqs;

public class FaqInsertRequest
{
    [Required(ErrorMessage = "A question is required.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "A question must be between 5 and 300 characters.")]
    public string Question { get; set; } = string.Empty;

    [Required(ErrorMessage = "An answer is required.")]
    [StringLength(2000, MinimumLength = 5, ErrorMessage = "An answer must be between 5 and 2000 characters.")]
    public string Answer { get; set; } = string.Empty;

    [Range(0, 1000, ErrorMessage = "Sort order must be between 0 and 1000.")]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
