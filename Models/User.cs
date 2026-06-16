namespace ServiceDeskPro.Models;

// This model represents every person who can login to the system.
// Examples are Admin, Manager, Technician and Employee.
public class User
{
    public int UserId { get; set; }
    public int CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee";
    public string Email { get; set; } = string.Empty;

    // Separate phone number makes technician contact details look like a real business system.
    public string Phone { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    // Technicians use this status so the dashboard can show if they are ready for new work.
    // Admin can change it to Available, On Break or Offline.
    public string AvailabilityStatus { get; set; } = "Available";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }

    // This is used on the Users page to show whether a login account is active.
    public string AccountStatus => IsActive ? "Active" : "Disabled";

    // This text is used in the Create Ticket technician dropdown.
    // It helps the admin assign work quickly because they can see the technician name,
    // department and current availability status in one line.
    // Example: "BONO SEAKAMELA - IT Support - Available".
    public string AssignmentDisplayName
    {
        get
        {
            string name = string.IsNullOrWhiteSpace(FullName) ? Username : FullName;
            string departmentText = string.IsNullOrWhiteSpace(Department) ? "No Department" : Department;
            string statusText = string.IsNullOrWhiteSpace(AvailabilityStatus) ? "Available" : AvailabilityStatus;
            return $"{name.ToUpper()} - {departmentText} - {statusText}";
        }
    }

    // This makes dropdowns show the user's real name instead of the class name
    // if a ComboBox forgets to set DisplayMemberPath.
    public override string ToString()
    {
        return AssignmentDisplayName;
    }
}
