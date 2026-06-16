namespace ServiceDeskPro.Models;

// Notifications show recent important system events like new tickets and assignments.
public class Notification
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public bool IsRead { get; set; }
}
