using Microsoft.Data.Sqlite;
using ServiceDeskPro.Models;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ServiceDeskPro.Services;

// This service manages the SQLite database file and common database helper methods.
public static class DatabaseService
{
    private static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    private static readonly string DatabasePath = Path.Combine(DataFolder, "servicedeskpro.db");
    private static readonly string ConnectionString = $"Data Source={DatabasePath};Pooling=False;Default Timeout=30";

    public static SqliteConnection GetConnection()
    {
        // Pooling is disabled in the connection string so SQLite releases the file quickly.
        // This prevents the common beginner issue: SQLite Error 5 - database is locked.
        return new SqliteConnection(ConnectionString);
    }

    private static void PrepareConnection(SqliteConnection connection)
    {
        // The busy timeout tells SQLite to wait a little if another database action is finishing.
        // WAL mode also helps the app read and write more safely during normal use.
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA busy_timeout = 5000; PRAGMA journal_mode = WAL;";
        command.ExecuteNonQuery();
    }

    public static void InitializeDatabase()
    {
        Directory.CreateDirectory(DataFolder);

        using var connection = GetConnection();
        connection.Open();
        PrepareConnection(connection);

        var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS Companies (
    CompanyId INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyName TEXT NOT NULL,
    Email TEXT NOT NULL,
    Phone TEXT NOT NULL,
    Address TEXT NOT NULL,
    CreatedDate TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Users (
    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
    CompanyId INTEGER NOT NULL,
    FullName TEXT NOT NULL,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Role TEXT NOT NULL,
    Email TEXT NOT NULL,
    Phone TEXT NOT NULL DEFAULT '',
    Department TEXT NOT NULL DEFAULT '',
    AvailabilityStatus TEXT NOT NULL DEFAULT 'Available',
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedDate TEXT NOT NULL,
    FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId)
);

CREATE TABLE IF NOT EXISTS Departments (
    DepartmentId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS Categories (
    CategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Description TEXT NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS Tickets (
    TicketId INTEGER PRIMARY KEY AUTOINCREMENT,
    TicketNumber TEXT NOT NULL UNIQUE,
    Title TEXT NOT NULL,
    Description TEXT NOT NULL,
    Category TEXT NOT NULL,
    Department TEXT NOT NULL,
    Priority TEXT NOT NULL,
    Status TEXT NOT NULL,
    CreatedByUserId INTEGER NOT NULL,
    AssignedToUserId INTEGER NULL,
    CreatedDate TEXT NOT NULL,
    DueDate TEXT NULL,
    ResolvedDate TEXT NULL,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    FOREIGN KEY (AssignedToUserId) REFERENCES Users(UserId)
);

CREATE TABLE IF NOT EXISTS TicketComments (
    CommentId INTEGER PRIMARY KEY AUTOINCREMENT,
    TicketId INTEGER NOT NULL,
    UserId INTEGER NOT NULL,
    CommentText TEXT NOT NULL,
    CreatedDate TEXT NOT NULL,
    FOREIGN KEY (TicketId) REFERENCES Tickets(TicketId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE IF NOT EXISTS Notifications (
    NotificationId INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Message TEXT NOT NULL,
    CreatedDate TEXT NOT NULL,
    IsRead INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS AuditLogs (
    AuditLogId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NULL,
    Action TEXT NOT NULL,
    Details TEXT NOT NULL,
    CreatedDate TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS SLARules (
    SlaRuleId INTEGER PRIMARY KEY AUTOINCREMENT,
    Priority TEXT NOT NULL UNIQUE,
    ResponseHours INTEGER NOT NULL,
    ResolutionHours INTEGER NOT NULL
);";
        command.ExecuteNonQuery();

        EnsureUserAvailabilityStatusColumn(connection);
        EnsureUserPhoneColumn(connection);
        SeedDefaultDepartments(connection);
        SeedDefaultCategories(connection);
        SeedDefaultSlaRules(connection);
    }


    private static void EnsureUserAvailabilityStatusColumn(SqliteConnection connection)
    {
        // This small migration keeps older database files working after we add the new technician status feature.
        bool columnExists = false;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(Users);";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(1).Equals("AvailabilityStatus", StringComparison.OrdinalIgnoreCase))
                {
                    columnExists = true;
                    break;
                }
            }
        }

        if (!columnExists)
        {
            using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE Users ADD COLUMN AvailabilityStatus TEXT NOT NULL DEFAULT 'Available';";
            alterCommand.ExecuteNonQuery();
        }

        using var updateCommand = connection.CreateCommand();
        updateCommand.CommandText = "UPDATE Users SET AvailabilityStatus = 'Available' WHERE AvailabilityStatus IS NULL OR TRIM(AvailabilityStatus) = '';";
        updateCommand.ExecuteNonQuery();
    }


    private static void EnsureUserPhoneColumn(SqliteConnection connection)
    {
        // This migration adds a separate Phone column for technician contact details.
        // Older database files will keep working because the column is added only if missing.
        bool columnExists = false;
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(Users);";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (reader.GetString(1).Equals("Phone", StringComparison.OrdinalIgnoreCase))
                {
                    columnExists = true;
                    break;
                }
            }
        }

        if (!columnExists)
        {
            using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = "ALTER TABLE Users ADD COLUMN Phone TEXT NOT NULL DEFAULT '';";
            alterCommand.ExecuteNonQuery();
        }
    }


    private static void SeedDefaultSlaRules(SqliteConnection connection)
    {
        // These SLA defaults are common in service desk systems.
        // Admins can change them in Settings without editing code.
        var defaults = new[]
        {
            new SlaRule { Priority = "Critical", ResponseHours = 1, ResolutionHours = 4 },
            new SlaRule { Priority = "High", ResponseHours = 2, ResolutionHours = 8 },
            new SlaRule { Priority = "Medium", ResponseHours = 8, ResolutionHours = 24 },
            new SlaRule { Priority = "Low", ResponseHours = 24, ResolutionHours = 72 }
        };

        foreach (var rule in defaults)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT OR IGNORE INTO SLARules (Priority, ResponseHours, ResolutionHours)
VALUES ($priority, $response, $resolution);";
            command.Parameters.AddWithValue("$priority", rule.Priority);
            command.Parameters.AddWithValue("$response", rule.ResponseHours);
            command.Parameters.AddWithValue("$resolution", rule.ResolutionHours);
            command.ExecuteNonQuery();
        }
    }

    public static List<SlaRule> GetSlaRules()
    {
        var rules = new List<SlaRule>();
        using var connection = GetConnection();
        connection.Open();
        PrepareConnection(connection);
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT SlaRuleId, Priority, ResponseHours, ResolutionHours
FROM SLARules
ORDER BY CASE Priority
    WHEN 'Critical' THEN 1
    WHEN 'High' THEN 2
    WHEN 'Medium' THEN 3
    WHEN 'Low' THEN 4
    ELSE 5
END;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            rules.Add(new SlaRule
            {
                SlaRuleId = reader.GetInt32(0),
                Priority = reader.GetString(1),
                ResponseHours = reader.GetInt32(2),
                ResolutionHours = reader.GetInt32(3)
            });
        }
        return rules;
    }

    public static void SaveSlaRules(IEnumerable<SlaRule> rules)
    {
        using var connection = GetConnection();
        connection.Open();
        PrepareConnection(connection);

        foreach (var rule in rules)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
UPDATE SLARules
SET ResponseHours = $response, ResolutionHours = $resolution
WHERE Priority = $priority;";
            command.Parameters.AddWithValue("$response", Math.Max(1, rule.ResponseHours));
            command.Parameters.AddWithValue("$resolution", Math.Max(1, rule.ResolutionHours));
            command.Parameters.AddWithValue("$priority", rule.Priority);
            command.ExecuteNonQuery();
        }
    }

    public static int GetResolutionHoursForPriority(string priority)
    {
        using var connection = GetConnection();
        connection.Open();
        PrepareConnection(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT ResolutionHours FROM SLARules WHERE Priority = $priority LIMIT 1";
        command.Parameters.AddWithValue("$priority", priority);
        var value = command.ExecuteScalar();
        return value == null ? 72 : Convert.ToInt32(value);
    }

    public static bool CompanyExists()
    {
        using var connection = GetConnection();
        connection.Open();
        PrepareConnection(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Companies";
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public static Company? GetCompany()
    {
        using var connection = GetConnection();
        connection.Open();
        PrepareConnection(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CompanyId, CompanyName, Email, Phone, Address, CreatedDate FROM Companies LIMIT 1";
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        return new Company
        {
            CompanyId = reader.GetInt32(0),
            CompanyName = reader.GetString(1),
            Email = reader.GetString(2),
            Phone = reader.GetString(3),
            Address = reader.GetString(4),
            CreatedDate = DateTime.Parse(reader.GetString(5))
        };
    }

    public static int RegisterCompanyAndAdmin(Company company, User admin, string plainPassword)
    {
        using var connection = GetConnection();
        connection.Open();
        PrepareConnection(connection);
        using var transaction = connection.BeginTransaction();

        var companyCommand = connection.CreateCommand();
        companyCommand.Transaction = transaction;
        companyCommand.CommandText = @"
INSERT INTO Companies (CompanyName, Email, Phone, Address, CreatedDate)
VALUES ($name, $email, $phone, $address, $created);
SELECT last_insert_rowid();";
        companyCommand.Parameters.AddWithValue("$name", company.CompanyName);
        companyCommand.Parameters.AddWithValue("$email", company.Email);
        companyCommand.Parameters.AddWithValue("$phone", company.Phone);
        companyCommand.Parameters.AddWithValue("$address", company.Address);
        companyCommand.Parameters.AddWithValue("$created", DateTime.Now.ToString("O"));
        int companyId = Convert.ToInt32(companyCommand.ExecuteScalar());

        var userCommand = connection.CreateCommand();
        userCommand.Transaction = transaction;
        userCommand.CommandText = @"
INSERT INTO Users (CompanyId, FullName, Username, PasswordHash, Role, Email, Phone, Department, AvailabilityStatus, IsActive, CreatedDate)
VALUES ($companyId, $fullName, $username, $passwordHash, 'Admin', $email, '', 'IT Administration', 'Available', 1, $created);";
        userCommand.Parameters.AddWithValue("$companyId", companyId);
        userCommand.Parameters.AddWithValue("$fullName", admin.FullName);
        userCommand.Parameters.AddWithValue("$username", admin.Username);
        userCommand.Parameters.AddWithValue("$passwordHash", HashPassword(plainPassword));
        userCommand.Parameters.AddWithValue("$email", admin.Email);
        userCommand.Parameters.AddWithValue("$created", DateTime.Now.ToString("O"));
        userCommand.ExecuteNonQuery();

        transaction.Commit();
        return companyId;
    }

    public static void UpdateCompany(Company company)
    {
        // This lets the admin update the organisation profile after registration.
        using var connection = GetConnection();
        connection.Open();
        PrepareConnection(connection);

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE Companies
SET CompanyName = $name, Email = $email, Phone = $phone, Address = $address
WHERE CompanyId = $companyId";
        command.Parameters.AddWithValue("$name", company.CompanyName);
        command.Parameters.AddWithValue("$email", company.Email);
        command.Parameters.AddWithValue("$phone", company.Phone);
        command.Parameters.AddWithValue("$address", company.Address);
        command.Parameters.AddWithValue("$companyId", company.CompanyId);
        command.ExecuteNonQuery();

        AddAuditLog(SessionService.CurrentUser?.UserId, "Update Company", "Updated company profile details.");
    }

    public static string HashPassword(string password)
    {
        // SHA256 is used here to keep the project simple for learning purposes.
        // In production, use a slow password hasher such as BCrypt or Argon2.
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }

    public static void AddAuditLog(int? userId, string action, string details)
    {
        // Audit logs are helpful, but they must never crash the app.
        // If SQLite is busy, we retry shortly instead of throwing an error to the user.
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                PrepareConnection(connection);

                using var command = connection.CreateCommand();
                command.CommandText = @"
INSERT INTO AuditLogs (UserId, Action, Details, CreatedDate)
VALUES ($userId, $action, $details, $created);";
                command.Parameters.AddWithValue("$userId", userId.HasValue ? userId.Value : DBNull.Value);
                command.Parameters.AddWithValue("$action", action);
                command.Parameters.AddWithValue("$details", details);
                command.Parameters.AddWithValue("$created", DateTime.Now.ToString("O"));
                command.ExecuteNonQuery();
                return;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 5 && attempt < 3)
            {
                Thread.Sleep(200);
            }
            catch
            {
                // The main action already succeeded, so we silently skip failed logging.
                return;
            }
        }
    }


    public static void CleanupPortfolioDemoData()
    {
        // Older test builds loaded sample technicians and sample tickets automatically.
        // This cleanup removes only those known sample records so the installed system starts with the real user's own data.
        using var connection = GetConnection();
        connection.Open();
        PrepareConnection(connection);

        using var transaction = connection.BeginTransaction();

        string[] demoTitles =
        [
            "Printer not working in Finance office",
            "VPN access issue for remote staff",
            "Suspicious email reported by HR",
            "New laptop setup request"
        ];

        foreach (string title in demoTitles)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Tickets WHERE Title = $title";
            command.Parameters.AddWithValue("$title", title);
            command.ExecuteNonQuery();
        }

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Notifications WHERE Title = 'Demo data loaded'";
            command.ExecuteNonQuery();
        }

        string[] demoUsernames = ["bono.tech", "brenda.tech", "jane.tech", "netshedzo.tech"];
        foreach (string username in demoUsernames)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Users WHERE Username = $username AND Role = 'Technician'";
            command.Parameters.AddWithValue("$username", username);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public static void SeedPortfolioDemoDataIfEmpty()
    {
        // This method gives a fresh installation realistic portfolio data.
        // It only seeds data when there are no technicians or no tickets, so it will not duplicate real work.
        var company = GetCompany();
        if (company == null) return;

        using var connection = GetConnection();
        connection.Open();
        PrepareConnection(connection);

        int adminUserId = 1;
        using (var adminCommand = connection.CreateCommand())
        {
            adminCommand.CommandText = "SELECT UserId FROM Users WHERE Role = 'Admin' ORDER BY UserId LIMIT 1";
            adminUserId = Convert.ToInt32(adminCommand.ExecuteScalar() ?? 1);
        }

        long technicianCount;
        using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = "SELECT COUNT(*) FROM Users WHERE Role = 'Technician'";
            technicianCount = (long)(countCommand.ExecuteScalar() ?? 0L);
        }

        if (technicianCount == 0)
        {
            AddDemoTechnician(connection, company.CompanyId, "BONO SEAKAMELA", "bono.tech", "bono@servicedesk.local", "IT Support");
            AddDemoTechnician(connection, company.CompanyId, "BRENDA NENGUDA", "brenda.tech", "brenda@servicedesk.local", "Network");
            AddDemoTechnician(connection, company.CompanyId, "JANE NENGUDA", "jane.tech", "jane@servicedesk.local", "Security");
            AddDemoTechnician(connection, company.CompanyId, "NETSHEDZO", "netshedzo.tech", "netshedzo@servicedesk.local", "Operations");
        }

        long ticketCount;
        using (var countCommand = connection.CreateCommand())
        {
            countCommand.CommandText = "SELECT COUNT(*) FROM Tickets";
            ticketCount = (long)(countCommand.ExecuteScalar() ?? 0L);
        }

        if (ticketCount == 0)
        {
            var technicians = new List<int>();
            using (var techCommand = connection.CreateCommand())
            {
                techCommand.CommandText = "SELECT UserId FROM Users WHERE Role = 'Technician' ORDER BY FullName LIMIT 4";
                using var reader = techCommand.ExecuteReader();
                while (reader.Read()) technicians.Add(reader.GetInt32(0));
            }

            AddDemoTicket(connection, adminUserId, technicians.ElementAtOrDefault(0), "Printer not working in Finance office", "The Finance printer shows a paper jam error even after clearing the tray.", "Printer", "Finance", "Medium", "Assigned", 24);
            AddDemoTicket(connection, adminUserId, technicians.ElementAtOrDefault(1), "VPN access issue for remote staff", "Remote employees cannot connect to the company VPN this morning.", "Network", "Network", "High", "In Progress", 8);
            AddDemoTicket(connection, adminUserId, technicians.ElementAtOrDefault(2), "Suspicious email reported by HR", "HR received an email asking for password reset details. Security review required.", "Security", "Human Resources", "Critical", "Open", 4);
            AddDemoTicket(connection, adminUserId, technicians.ElementAtOrDefault(3), "New laptop setup request", "Operations needs a new laptop prepared with standard business applications.", "Hardware", "Operations", "Low", "Resolved", 72, true);

            InsertNotification(connection, "Demo data loaded", "Sample service desk tickets were added for your portfolio dashboard.");
        }
    }

    private static void AddDemoTechnician(SqliteConnection connection, int companyId, string fullName, string username, string email, string department)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT OR IGNORE INTO Users (CompanyId, FullName, Username, PasswordHash, Role, Email, Department, AvailabilityStatus, IsActive, CreatedDate)
VALUES ($companyId, $fullName, $username, $passwordHash, 'Technician', $email, $department, 'Available', 1, $created);";
        command.Parameters.AddWithValue("$companyId", companyId);
        command.Parameters.AddWithValue("$fullName", fullName);
        command.Parameters.AddWithValue("$username", username);
        command.Parameters.AddWithValue("$passwordHash", HashPassword("Password123!"));
        command.Parameters.AddWithValue("$email", email);
        command.Parameters.AddWithValue("$department", department);
        command.Parameters.AddWithValue("$created", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static void AddDemoTicket(SqliteConnection connection, int adminUserId, int technicianId, string title, string description, string category, string department, string priority, string status, int dueHours, bool resolved = false)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Tickets (TicketNumber, Title, Description, Category, Department, Priority, Status, CreatedByUserId, AssignedToUserId, CreatedDate, DueDate, ResolvedDate)
VALUES ($number, $title, $description, $category, $department, $priority, $status, $createdBy, $assignedTo, $created, $due, $resolved);";
        command.Parameters.AddWithValue("$number", $"TKT-{DateTime.Now:yyyy}-{GetNextDemoTicketNumber(connection):00000}");
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$description", description);
        command.Parameters.AddWithValue("$category", category);
        command.Parameters.AddWithValue("$department", department);
        command.Parameters.AddWithValue("$priority", priority);
        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$createdBy", adminUserId);
        command.Parameters.AddWithValue("$assignedTo", technicianId == 0 ? (object)DBNull.Value : technicianId);
        command.Parameters.AddWithValue("$created", DateTime.Now.AddHours(-2).ToString("O"));
        command.Parameters.AddWithValue("$due", DateTime.Now.AddHours(dueHours).ToString("O"));
        command.Parameters.AddWithValue("$resolved", resolved ? DateTime.Now.AddMinutes(-35).ToString("O") : (object)DBNull.Value);
        command.ExecuteNonQuery();
    }

    private static int GetNextDemoTicketNumber(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Tickets WHERE TicketNumber LIKE $year";
        command.Parameters.AddWithValue("$year", $"TKT-{DateTime.Now:yyyy}-%");
        return Convert.ToInt32(command.ExecuteScalar() ?? 0) + 1;
    }

    private static void InsertNotification(SqliteConnection connection, string title, string message)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Notifications (Title, Message, CreatedDate, IsRead) VALUES ($title, $message, $created, 0)";
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$message", message);
        command.Parameters.AddWithValue("$created", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static void SeedDefaultDepartments(SqliteConnection connection)
    {
        string[] departments = ["IT Support", "Network", "Security", "Finance", "Human Resources", "Operations"];

        foreach (string department in departments)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO Departments (Name, Description) VALUES ($name, $description)";
            command.Parameters.AddWithValue("$name", department);
            command.Parameters.AddWithValue("$description", $"Default {department} department");
            command.ExecuteNonQuery();
        }
    }

    private static void SeedDefaultCategories(SqliteConnection connection)
    {
        // Categories are stored in the database so each company can create its own real-world ticket types.
        string[] categories = ["Hardware", "Software", "Network", "Printer", "Email", "Security", "Other"];

        foreach (string category in categories)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO Categories (Name, Description) VALUES ($name, $description)";
            command.Parameters.AddWithValue("$name", category);
            command.Parameters.AddWithValue("$description", $"Default {category} ticket category");
            command.ExecuteNonQuery();
        }
    }
}
