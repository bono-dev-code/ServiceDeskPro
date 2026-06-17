namespace ServiceDeskPro.Models;

// This model represents one IT support request in the system.
public class Ticket
{
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public int CreatedByUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public int? AssignedToUserId { get; set; }
    public string AssignedToName { get; set; } = "Unassigned";
    public DateTime CreatedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ResolvedDate { get; set; }

    // This property is calculated for the UI so the admin can quickly see late tickets.
    public string SlaStatus
    {
        get
        {
            if (Status is "Resolved" or "Closed") return "Completed";
            if (DueDate.HasValue && DateTime.Now > DueDate.Value) return "Breached";
            return "On Track";
        }
    }
}
