using ServiceDeskPro.Models;

namespace ServiceDeskPro.Services;

// This service prepares dashboard statistics and reports from ticket data.
public static class ReportService
{
    public static DashboardStats GetDashboardStats()
    {
        var tickets = TicketService.GetTickets();
        return new DashboardStats
        {
            OpenTickets = tickets.Count(t => t.Status is "Open" or "Assigned"),
            InProgressTickets = tickets.Count(t => t.Status == "In Progress"),
            CriticalTickets = tickets.Count(t => t.Priority == "Critical" && t.Status is not "Closed"),
            ResolvedTickets = tickets.Count(t => t.Status is "Resolved" or "Closed"),
            SlaBreachedTickets = tickets.Count(t => t.SlaStatus == "Breached")
        };
    }

    public static List<TechnicianSummary> GetTechnicianSummaries()
    {
        var technicians = AuthService.GetUsers("Technician");
        var tickets = TicketService.GetTickets();

        return technicians.Select(t => new TechnicianSummary
        {
            UserId = t.UserId,
            FullName = t.FullName,
            Username = t.Username,
            Email = t.Email,
            Phone = t.Phone,
            Department = t.Department,
            AvailabilityStatus = t.AvailabilityStatus,
            AssignedTickets = tickets.Count(x => x.AssignedToUserId == t.UserId && x.Status is not "Closed"),
            ResolvedTickets = tickets.Count(x => x.AssignedToUserId == t.UserId && x.Status is "Resolved" or "Closed")
        }).ToList();
    }

    public static List<ReportRow> TicketsByStatus()
    {
        return TicketService.GetTickets()
            .GroupBy(t => t.Status)
            .Select(g => new ReportRow { Label = g.Key, Count = g.Count() })
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    public static List<ReportRow> TicketsByPriority()
    {
        return TicketService.GetTickets()
            .GroupBy(t => t.Priority)
            .Select(g => new ReportRow { Label = g.Key, Count = g.Count() })
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    public static List<ReportRow> TicketsByDepartment()
    {
        return TicketService.GetTickets()
            .GroupBy(t => t.Department)
            .Select(g => new ReportRow { Label = g.Key, Count = g.Count() })
            .OrderByDescending(r => r.Count)
            .ToList();
    }
    public static List<ReportRow> TechnicianWorkload()
    {
        return GetTechnicianSummaries()
            .Select(t => new ReportRow { Label = t.FullName, Count = t.AssignedTickets })
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    public static List<ReportRow> SlaSummary()
    {
        var tickets = TicketService.GetTickets();
        return tickets
            .GroupBy(t => t.SlaStatus)
            .Select(g => new ReportRow { Label = g.Key, Count = g.Count() })
            .OrderByDescending(r => r.Count)
            .ToList();
    }

    public static List<ReportRow> AddBars(List<ReportRow> rows)
    {
        int max = rows.Count == 0 ? 0 : rows.Max(r => r.Count);
        foreach (var row in rows)
        {
            int length = max == 0 ? 0 : Math.Max(1, (int)Math.Round((row.Count / (double)max) * 18));
            row.Bar = new string('█', length);
        }
        return rows;
    }

}
