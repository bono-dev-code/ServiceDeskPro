using ServiceDeskPro.Models;

namespace ServiceDeskPro.Services;

// This service handles login and user account actions.
public static class AuthService
{
    public static User? Login(string username, string password)
    {
        using var connection = DatabaseService.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT UserId, CompanyId, FullName, Username, PasswordHash, Role, Email, Phone, Department, AvailabilityStatus, IsActive, CreatedDate
FROM Users
WHERE Username = $username AND PasswordHash = $passwordHash AND IsActive = 1";
        command.Parameters.AddWithValue("$username", username.Trim());
        command.Parameters.AddWithValue("$passwordHash", DatabaseService.HashPassword(password));

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        var user = new User
        {
            UserId = reader.GetInt32(0),
            CompanyId = reader.GetInt32(1),
            FullName = reader.GetString(2),
            Username = reader.GetString(3),
            PasswordHash = reader.GetString(4),
            Role = reader.GetString(5),
            Email = reader.GetString(6),
            Phone = reader.GetString(7),
            Department = reader.GetString(8),
            AvailabilityStatus = reader.GetString(9),
            IsActive = reader.GetInt32(10) == 1,
            CreatedDate = DateTime.Parse(reader.GetString(11))
        };

        SessionService.CurrentUser = user;
        SessionService.CurrentCompany = DatabaseService.GetCompany();
        DatabaseService.AddAuditLog(user.UserId, "Login", $"{user.FullName} logged in.");
        return user;
    }

    public static void AddUser(User user, string plainPassword)
    {
        int companyId = SessionService.CurrentCompany?.CompanyId ?? DatabaseService.GetCompany()?.CompanyId ?? 1;

        // The Username column in the database must be unique.
        // If the user types a username that already exists, this method creates a safe new one
        // instead of letting SQLite crash with: UNIQUE constraint failed.
        user.Username = CreateUniqueUsername(user.Username, user.FullName);

        // Email can be empty for internal technician accounts, so we store a safe placeholder.
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            user.Email = $"{user.Username}@servicedesk.local";
        }

        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Users (CompanyId, FullName, Username, PasswordHash, Role, Email, Phone, Department, AvailabilityStatus, IsActive, CreatedDate)
VALUES ($companyId, $fullName, $username, $passwordHash, $role, $email, $phone, $department, $availabilityStatus, 1, $created);";
        command.Parameters.AddWithValue("$companyId", companyId);
        command.Parameters.AddWithValue("$fullName", user.FullName);
        command.Parameters.AddWithValue("$username", user.Username);
        command.Parameters.AddWithValue("$passwordHash", DatabaseService.HashPassword(plainPassword));
        command.Parameters.AddWithValue("$role", user.Role);
        command.Parameters.AddWithValue("$email", user.Email);
        command.Parameters.AddWithValue("$phone", user.Phone ?? string.Empty);
        command.Parameters.AddWithValue("$department", user.Department);
        command.Parameters.AddWithValue("$availabilityStatus", string.IsNullOrWhiteSpace(user.AvailabilityStatus) ? "Available" : user.AvailabilityStatus);
        command.Parameters.AddWithValue("$created", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();

        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Create User", $"Created {user.Role}: {user.FullName} ({user.Username})");
    }

    private static string CreateUniqueUsername(string requestedUsername, string fullName)
    {
        // Start with the typed username. If it is empty, build one from the full name.
        string baseUsername = string.IsNullOrWhiteSpace(requestedUsername)
            ? fullName.Trim().ToLower().Replace(" ", ".")
            : requestedUsername.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(baseUsername))
        {
            baseUsername = "user";
        }

        string usernameToTry = baseUsername;
        int number = 2;

        // Keep adding a number until we find a username that is not already in the database.
        while (UsernameExists(usernameToTry))
        {
            usernameToTry = $"{baseUsername}{number}";
            number++;
        }

        return usernameToTry;
    }

    private static bool UsernameExists(string username)
    {
        using var connection = DatabaseService.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users WHERE LOWER(Username) = LOWER($username);";
        command.Parameters.AddWithValue("$username", username);

        long count = (long)(command.ExecuteScalar() ?? 0L);
        return count > 0;
    }

    public static List<User> GetUsers(string? role = null)
    {
        var users = new List<User>();
        using var connection = DatabaseService.GetConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = string.IsNullOrWhiteSpace(role)
            ? "SELECT UserId, CompanyId, FullName, Username, PasswordHash, Role, Email, Phone, Department, AvailabilityStatus, IsActive, CreatedDate FROM Users WHERE IsActive = 1 ORDER BY FullName"
            : "SELECT UserId, CompanyId, FullName, Username, PasswordHash, Role, Email, Phone, Department, AvailabilityStatus, IsActive, CreatedDate FROM Users WHERE Role = $role AND IsActive = 1 ORDER BY FullName";
        if (!string.IsNullOrWhiteSpace(role)) command.Parameters.AddWithValue("$role", role);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new User
            {
                UserId = reader.GetInt32(0),
                CompanyId = reader.GetInt32(1),
                FullName = reader.GetString(2),
                Username = reader.GetString(3),
                PasswordHash = reader.GetString(4),
                Role = reader.GetString(5),
                Email = reader.GetString(6),
                Phone = reader.GetString(7),
                Department = reader.GetString(8),
                AvailabilityStatus = reader.GetString(9),
                IsActive = reader.GetInt32(10) == 1,
                CreatedDate = DateTime.Parse(reader.GetString(11))
            });
        }
        return users;
    }

    public static void UpdateTechnicianAvailability(int userId, string availabilityStatus)
    {
        // Admin uses this method to change if a technician is Available, On Break or Offline.
        string safeStatus = availabilityStatus is "Available" or "On Break" or "Offline" ? availabilityStatus : "Available";

        using var connection = DatabaseService.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Users SET AvailabilityStatus = $status WHERE UserId = $userId AND Role = 'Technician';";
        command.Parameters.AddWithValue("$status", safeStatus);
        command.Parameters.AddWithValue("$userId", userId);
        command.ExecuteNonQuery();

        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Update Technician Status", $"Changed technician ID {userId} status to {safeStatus}.");
    }


    public static void UpdateTechnician(User technician)
    {
        // Admin uses this method from the Technician Management page to update a technician profile.
        // Assigned ticket counts are not edited here because they are calculated automatically from tickets.
        string safeStatus = technician.AvailabilityStatus is "Available" or "On Break" or "Offline" ? technician.AvailabilityStatus : "Available";

        using var connection = DatabaseService.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE Users
SET FullName = $fullName,
    Email = $email,
    Phone = $phone,
    Department = $department,
    AvailabilityStatus = $status
WHERE UserId = $userId AND Role = 'Technician' AND IsActive = 1;";
        command.Parameters.AddWithValue("$fullName", technician.FullName.Trim());
        command.Parameters.AddWithValue("$email", technician.Email.Trim());
        command.Parameters.AddWithValue("$phone", technician.Phone.Trim());
        command.Parameters.AddWithValue("$department", technician.Department.Trim());
        command.Parameters.AddWithValue("$status", safeStatus);
        command.Parameters.AddWithValue("$userId", technician.UserId);
        command.ExecuteNonQuery();

        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Update Technician", $"Updated technician: {technician.FullName}.");
    }

    public static void DeleteTechnician(int userId)
    {
        // This is a safe delete. The account is deactivated instead of being permanently removed.
        // Existing tickets are unassigned so deleted technicians no longer appear as active workload.
        using var connection = DatabaseService.GetConnection();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        using var unassignCommand = connection.CreateCommand();
        unassignCommand.Transaction = transaction;
        unassignCommand.CommandText = "UPDATE Tickets SET AssignedToUserId = NULL, Status = CASE WHEN Status = 'Assigned' THEN 'Open' ELSE Status END WHERE AssignedToUserId = $userId AND Status NOT IN ('Resolved', 'Closed');";
        unassignCommand.Parameters.AddWithValue("$userId", userId);
        unassignCommand.ExecuteNonQuery();

        using var deactivateCommand = connection.CreateCommand();
        deactivateCommand.Transaction = transaction;
        deactivateCommand.CommandText = "UPDATE Users SET IsActive = 0, AvailabilityStatus = 'Offline' WHERE UserId = $userId AND Role = 'Technician';";
        deactivateCommand.Parameters.AddWithValue("$userId", userId);
        deactivateCommand.ExecuteNonQuery();

        transaction.Commit();

        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Delete Technician", $"Deactivated technician ID {userId}.");
    }


    public static void DisableUser(int userId)
    {
        // Users page disables login accounts. If the user is also a technician,
        // their technician availability is moved to Offline so they cannot receive work.
        using var connection = DatabaseService.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Users SET IsActive = 0, AvailabilityStatus = 'Offline' WHERE UserId = $userId;";
        command.Parameters.AddWithValue("$userId", userId);
        command.ExecuteNonQuery();

        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Disable User", $"Disabled user account ID {userId}.");
    }

    public static void ResetPassword(int userId, string temporaryPassword)
    {
        // Admin can reset a user's password without changing their profile details.
        using var connection = DatabaseService.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Users SET PasswordHash = $passwordHash WHERE UserId = $userId AND IsActive = 1;";
        command.Parameters.AddWithValue("$passwordHash", DatabaseService.HashPassword(temporaryPassword));
        command.Parameters.AddWithValue("$userId", userId);
        command.ExecuteNonQuery();

        DatabaseService.AddAuditLog(SessionService.CurrentUser?.UserId, "Reset Password", $"Reset password for user account ID {userId}.");
    }


    public static User? FindActiveUserByUsernameAndEmail(string username, string email)
    {
        // This method supports the Forgot Password flow.
        // The user must provide both username and email so another person cannot reset a password using only a username.
        using var connection = DatabaseService.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT UserId, CompanyId, FullName, Username, PasswordHash, Role, Email, Phone, Department, AvailabilityStatus, IsActive, CreatedDate
FROM Users
WHERE LOWER(Username) = LOWER($username)
  AND LOWER(Email) = LOWER($email)
  AND IsActive = 1;";
        command.Parameters.AddWithValue("$username", username.Trim());
        command.Parameters.AddWithValue("$email", email.Trim());

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        return new User
        {
            UserId = reader.GetInt32(0),
            CompanyId = reader.GetInt32(1),
            FullName = reader.GetString(2),
            Username = reader.GetString(3),
            PasswordHash = reader.GetString(4),
            Role = reader.GetString(5),
            Email = reader.GetString(6),
            Phone = reader.GetString(7),
            Department = reader.GetString(8),
            AvailabilityStatus = reader.GetString(9),
            IsActive = reader.GetInt32(10) == 1,
            CreatedDate = DateTime.Parse(reader.GetString(11))
        };
    }

    public static void ResetPasswordByForgotPassword(int userId, string newPassword)
    {
        // This method is used after the user has verified their username and email.
        // It updates only the password hash and keeps all profile details unchanged.
        using var connection = DatabaseService.GetConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Users SET PasswordHash = $passwordHash WHERE UserId = $userId AND IsActive = 1;";
        command.Parameters.AddWithValue("$passwordHash", DatabaseService.HashPassword(newPassword));
        command.Parameters.AddWithValue("$userId", userId);
        command.ExecuteNonQuery();

        DatabaseService.AddAuditLog(userId, "Forgot Password", "User reset their password using username and email verification.");
    }

}
