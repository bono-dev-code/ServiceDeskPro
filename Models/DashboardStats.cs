namespace ServiceDeskPro.Models;

// This class holds the numbers shown on the dashboard cards.
public class DashboardStats
{
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int CriticalTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int SlaBreachedTickets { get; set; }
}
