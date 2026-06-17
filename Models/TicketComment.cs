namespace ServiceDeskPro.Models;

// Comments are used to record conversations and progress notes inside a ticket.
public class TicketComment
{
    public int CommentId { get; set; }
    public int TicketId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string CommentText { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
