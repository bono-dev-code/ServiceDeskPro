using ServiceDeskPro.Models;

namespace ServiceDeskPro.Services;

// This service contains all ticket-related database operations.
public static class TicketService
{
    public static List<string> GetDepartments()
    {
        var departments = new List<string>();
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name FROM Departments ORDER BY Name";
        using var reader = command.ExecuteReader();
        while (reader.Read()) departments.Add(reader.GetString(0));
        return departments;
    }

    public static List<string> GetCategories()
    {
        // Categories are loaded from the database, not hardcoded.
        // This lets any company create ticket categories that match how they really work.
        var categories = new List<string>();
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Name FROM Categories ORDER BY Name";
        using var reader = command.ExecuteReader();
        while (reader.Read()) categories.Add(reader.GetString(0));
        return categories;
    }

    public static void AddDepartment(string name)
    {
        string cleanName = name.Trim();
        if (string.IsNullOrWhiteSpace(cleanName)) return;

        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO Departments (Name, Description) VALUES ($name, '')";
        command.Parameters.AddWithValue("$name", cleanName);
        command.ExecuteNonQuery();

        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Create Department", $"Added department: {cleanName}");
    }

    public static void DeleteDepartment(string name)
    {
        string cleanName = name.Trim();
        if (string.IsNullOrWhiteSpace(cleanName)) return;

        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Departments WHERE Name = $name";
        command.Parameters.AddWithValue("$name", cleanName);
        command.ExecuteNonQuery();

        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Delete Department", $"Deleted department: {cleanName}");
    }

    public static void AddCategory(string name)
    {
        string cleanName = name.Trim();
        if (string.IsNullOrWhiteSpace(cleanName)) return;

        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO Categories (Name, Description) VALUES ($name, '')";
        command.Parameters.AddWithValue("$name", cleanName);
        command.ExecuteNonQuery();

        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Create Category", $"Added ticket category: {cleanName}");
    }

    public static void DeleteCategory(string name)
    {
        string cleanName = name.Trim();
        if (string.IsNullOrWhiteSpace(cleanName)) return;

        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Categories WHERE Name = $name";
        command.Parameters.AddWithValue("$name", cleanName);
        command.ExecuteNonQuery();

        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Delete Category", $"Deleted ticket category: {cleanName}");
    }

    public static int CreateTicket(Ticket ticket)
    {
        ticket.TicketNumber = GenerateTicketNumber();
        ticket.CreatedDate = DateTime.Now;
        ticket.DueDate = CalculateDueDate(ticket.Priority);

        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Tickets (TicketNumber, Title, Description, Category, Department, Priority, Status, CreatedByUserId, AssignedToUserId, CreatedDate, DueDate, ResolvedDate)
VALUES ($number, $title, $description, $category, $department, $priority, $status, $createdBy, $assignedTo, $created, $due, NULL);
SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$number", ticket.TicketNumber);
        command.Parameters.AddWithValue("$title", ticket.Title);
        command.Parameters.AddWithValue("$description", ticket.Description);
        command.Parameters.AddWithValue("$category", ticket.Category);
        command.Parameters.AddWithValue("$department", ticket.Department);
        command.Parameters.AddWithValue("$priority", ticket.Priority);
        command.Parameters.AddWithValue("$status", ticket.AssignedToUserId.HasValue ? "Assigned" : "Open");
        command.Parameters.AddWithValue("$createdBy", ticket.CreatedByUserId);
        command.Parameters.AddWithValue("$assignedTo", ticket.AssignedToUserId.HasValue ? ticket.AssignedToUserId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$created", ticket.CreatedDate.ToString("O"));
        command.Parameters.AddWithValue("$due", ticket.DueDate?.ToString("O") ?? (object)DBNull.Value);
        int ticketId = Convert.ToInt32(command.ExecuteScalar());

        AddNotification("New Ticket Created", $"{ticket.TicketNumber} - {ticket.Title}");
        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Create Ticket", $"Created ticket {ticket.TicketNumber}");
        return ticketId;
    }

    public static List<Ticket> GetTickets(string search = "")
    {
        var tickets = new List<Ticket>();
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT t.TicketId, t.TicketNumber, t.Title, t.Description, t.Category, t.Department, t.Priority, t.Status,
       t.CreatedByUserId, creator.FullName, t.AssignedToUserId, COALESCE(tech.FullName, 'Unassigned'),
       t.CreatedDate, t.DueDate, t.ResolvedDate
FROM Tickets t
JOIN Users creator ON creator.UserId = t.CreatedByUserId
LEFT JOIN Users tech ON tech.UserId = t.AssignedToUserId
WHERE $search = '' OR t.TicketNumber LIKE $like OR t.Title LIKE $like OR t.Priority LIKE $like OR t.Status LIKE $like
ORDER BY t.CreatedDate DESC";
        command.Parameters.AddWithValue("$search", search.Trim());
        command.Parameters.AddWithValue("$like", $"%{search.Trim()}%");

        using var reader = command.ExecuteReader();
        while (reader.Read()) tickets.Add(ReadTicket(reader));
        return tickets;
    }

    public static void AssignTicket(int ticketId, int technicianId)
    {
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Tickets SET AssignedToUserId = $tech, Status = 'Assigned' WHERE TicketId = $ticketId";
        command.Parameters.AddWithValue("$tech", technicianId);
        command.Parameters.AddWithValue("$ticketId", ticketId);
        command.ExecuteNonQuery();

        AddNotification("Ticket Assigned", $"Ticket #{ticketId} was assigned to a technician.");
        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Assign Ticket", $"Assigned ticket ID {ticketId}");
    }

    public static void UpdateStatus(int ticketId, string status)
    {
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = status is "Resolved" or "Closed"
            ? "UPDATE Tickets SET Status = $status, ResolvedDate = $resolved WHERE TicketId = $ticketId"
            : "UPDATE Tickets SET Status = $status WHERE TicketId = $ticketId";
        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$ticketId", ticketId);
        if (status is "Resolved" or "Closed") command.Parameters.AddWithValue("$resolved", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();

        AddNotification("Ticket Status Changed", $"Ticket #{ticketId} status changed to {status}.");
        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Update Status", $"Ticket ID {ticketId} changed to {status}");
    }

    public static void AddComment(int ticketId, int userId, string comment)
    {
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO TicketComments (TicketId, UserId, CommentText, CreatedDate)
VALUES ($ticketId, $userId, $comment, $created)";
        command.Parameters.AddWithValue("$ticketId", ticketId);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$comment", comment);
        command.Parameters.AddWithValue("$created", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();
    }

    public static List<TicketComment> GetComments(int ticketId)
    {
        var comments = new List<TicketComment>();
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT c.CommentId, c.TicketId, c.UserId, u.FullName, c.CommentText, c.CreatedDate
FROM TicketComments c
JOIN Users u ON u.UserId = c.UserId
WHERE c.TicketId = $ticketId
ORDER BY c.CreatedDate DESC";
        command.Parameters.AddWithValue("$ticketId", ticketId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            comments.Add(new TicketComment
            {
                CommentId = reader.GetInt32(0),
                TicketId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                UserName = reader.GetString(3),
                CommentText = reader.GetString(4),
                CreatedDate = DateTime.Parse(reader.GetString(5))
            });
        }
        return comments;
    }

    public static List<Notification> GetNotifications(int limit = 8)
    {
        var notifications = new List<Notification>();
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT NotificationId, Title, Message, CreatedDate, IsRead FROM Notifications ORDER BY CreatedDate DESC LIMIT $limit";
        command.Parameters.AddWithValue("$limit", limit);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            notifications.Add(new Notification
            {
                NotificationId = reader.GetInt32(0),
                Title = reader.GetString(1),
                Message = reader.GetString(2),
                CreatedDate = DateTime.Parse(reader.GetString(3)),
                IsRead = reader.GetInt32(4) == 1
            });
        }
        return notifications;
    }

    private static Ticket ReadTicket(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new Ticket
        {
            TicketId = reader.GetInt32(0),
            TicketNumber = reader.GetString(1),
            Title = reader.GetString(2),
            Description = reader.GetString(3),
            Category = reader.GetString(4),
            Department = reader.GetString(5),
            Priority = reader.GetString(6),
            Status = reader.GetString(7),
            CreatedByUserId = reader.GetInt32(8),
            CreatedByName = reader.GetString(9),
            AssignedToUserId = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            AssignedToName = reader.GetString(11),
            CreatedDate = DateTime.Parse(reader.GetString(12)),
            DueDate = reader.IsDBNull(13) ? null : DateTime.Parse(reader.GetString(13)),
            ResolvedDate = reader.IsDBNull(14) ? null : DateTime.Parse(reader.GetString(14))
        };
    }

    private static string GenerateTicketNumber()
    {
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Tickets WHERE TicketNumber LIKE $year";
        command.Parameters.AddWithValue("$year", $"TKT-{DateTime.Now:yyyy}-%");
        int count = Convert.ToInt32(command.ExecuteScalar()) + 1;
        return $"TKT-{DateTime.Now:yyyy}-{count:00000}";
    }

    private static DateTime CalculateDueDate(string priority)
    {
        // The due date now comes from the editable SLA rules in Settings.
        // Example: Critical can be changed from 4 hours to any company value.
        int resolutionHours = DatabaseService.GetResolutionHoursForPriority(priority);
        return DateTime.Now.AddHours(resolutionHours);
    }

    private static void AddNotification(string title, string message)
    {
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Notifications (Title, Message, CreatedDate, IsRead) VALUES ($title, $message, $created, 0)";
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$message", message);
        command.Parameters.AddWithValue("$created", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();
    }
}
