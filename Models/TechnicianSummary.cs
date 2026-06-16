namespace ServiceDeskPro.Models;

// This class is used for the technician list on the right side of the dashboard.
public class TechnicianSummary
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int AssignedTickets { get; set; }
    public int ResolvedTickets { get; set; }

    // This status is controlled by the admin from the Technicians page.
    // It is shown on the dashboard so the company can see who is available.
    public string AvailabilityStatus { get; set; } = "Available";

    // Display text used in tables and dashboard badges.
    public string Availability => AvailabilityStatus;


    // Shows the technician name in a neat business format on tables.
    public string DisplayName => ToTitleCase(FullName);

    // Shows the department in a neat business format on tables.
    public string DisplayDepartment => string.IsNullOrWhiteSpace(Department) ? "Unassigned" : ToTitleCase(Department);

    // This keeps names professional even if the user typed them in lowercase.
    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower());
    }

    // Initials are used by the dashboard avatar circles.
    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][0].ToString().ToUpper();
            return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
        }
    }
}
