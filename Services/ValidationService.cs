using System.Text.RegularExpressions;

namespace ServiceDeskPro.Services;

// This helper keeps validation rules in one place.
// Beginner note: instead of copying email and phone checks into every window,
// the screens call this service whenever they need to validate user input.
public static class ValidationService
{
    // Basic but practical email pattern for business software.
    // It checks that the email has text before @, a domain, and a normal extension.
    private static readonly Regex EmailRegex = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // South African-style phone rule used in this project:
    // exactly 10 digits and starting with 0, for example 0760470006.
    private static readonly Regex PhoneRegex = new(
        @"^0\d{9}$",
        RegexOptions.Compiled);

    public static bool IsValidEmail(string? email)
    {
        return !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());
    }

    public static bool IsValidPhone(string? phone)
    {
        return !string.IsNullOrWhiteSpace(phone) && PhoneRegex.IsMatch(phone.Trim());
    }

    public static string EmailError(string fieldName)
    {
        return $"{fieldName} must be a valid email address, for example name@company.com.";
    }

    public static string PhoneError(string fieldName)
    {
        return $"{fieldName} must be 10 digits, numbers only, and start with 0. Example: 0760470006.";
    }
}
