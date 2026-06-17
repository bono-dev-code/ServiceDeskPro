using ServiceDeskPro.Models;

namespace ServiceDeskPro.Services;

// This small service keeps track of the user who is currently logged in.
public static class SessionService
{
    public static User? CurrentUser { get; set; }
    public static Company? CurrentCompany { get; set; }

    public static void Clear()
    {
        CurrentUser = null;
        CurrentCompany = null;
    }
}
