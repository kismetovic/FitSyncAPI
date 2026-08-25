using System.ComponentModel.DataAnnotations;

namespace FITSync.Contracts.Notifications;

public class SendPaymentReminderRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "A valid user must be selected.")]
    public int UserId { get; set; }
}
