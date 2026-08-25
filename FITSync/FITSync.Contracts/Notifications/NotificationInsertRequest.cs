using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Notifications;

/// <summary>Administrative broadcast of a notification to a specific user.</summary>
public class NotificationInsertRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 120 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required.")]
    [StringLength(1000, MinimumLength = 2, ErrorMessage = "Message must be between 2 and 1000 characters.")]
    public string Message { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A valid user must be selected.")]
    public int UserId { get; set; }
}
