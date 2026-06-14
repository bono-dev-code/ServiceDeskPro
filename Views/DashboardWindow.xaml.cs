using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ServiceDeskPro.Models;
using ServiceDeskPro.Services;

namespace ServiceDeskPro.Views;

public partial class DashboardWindow : Window
{
    private string _currentReportTitle = "Monthly Service Desk Report";
    private string _currentReportText = string.Empty;
    private List<ReportRow> _currentReportRows = new();

    public DashboardWindow()
    {
        InitializeComponent();

        // The system starts clean after registration.
        // Real users add their own technicians and tickets so the software feels realistic.
        DatabaseService.CleanupPortfolioDemoData();

        LoadStaticLists();
        LoadAllData();
    }

    private void LoadStaticLists()
    {
        // Priority is static for now, while Categories and Departments are configurable in Settings.
        PriorityBox.ItemsSource = new[] { "Low", "Medium", "High", "Critical" };
        var availabilityStatuses = new[] { "Available", "On Break", "Offline" };
        QuickTechStatusBox.ItemsSource = availabilityStatuses;
        if (SystemUserRoleBox != null) SystemUserRoleBox.SelectedIndex = 2;
        PriorityBox.SelectedIndex = 1;
        QuickTechStatusBox.SelectedIndex = 0;
    }

    private void LoadAllData()
    {
        // One method refreshes the whole dashboard after changes like creating a ticket.
        var user = SessionService.CurrentUser;
        var company = SessionService.CurrentCompany;

        GreetingText.Text = $"Hello, {user?.FullName ?? "Admin"} 👋";
        CompanyText.Text = company == null ? "Welcome to ServiceDesk Pro." : $"Welcome to {company.CompanyName} IT Support Dashboard.";
        UserText.Text = $"{user?.FullName}  |  {user?.Role}";
        UserInitialText.Text = string.IsNullOrWhiteSpace(user?.FullName) ? "A" : user.FullName.Trim()[0].ToString().ToUpper();

        CategoryBox.ItemsSource = TicketService.GetCategories();
        DepartmentBox.ItemsSource = TicketService.GetDepartments();
        TechDepartmentBox.ItemsSource = TicketService.GetDepartments();
        CategoryBox.SelectedIndex = CategoryBox.Items.Count > 0 ? 0 : -1;
        DepartmentBox.SelectedIndex = DepartmentBox.Items.Count > 0 ? 0 : -1;
        TechDepartmentBox.SelectedIndex = TechDepartmentBox.Items.Count > 0 ? 0 : -1;

        // The assignment dropdown must be clear for the admin.
        // It now shows: NAME - DEPARTMENT - STATUS, for example:
        // "BONO SEAKAMELA - IT Support - Available".
        // Available technicians are listed first so ticket assignment is faster.
        AssignTechBox.DisplayMemberPath = "AssignmentDisplayName";
        AssignTechBox.SelectedValuePath = "UserId";
        AssignTechBox.ItemsSource = AuthService.GetUsers("Technician")
            .OrderBy(t => t.AvailabilityStatus == "Available" ? 0 : t.AvailabilityStatus == "On Break" ? 1 : 2)
            .ThenBy(t => t.FullName)
            .ToList();


        LoadDashboard();
        LoadTickets();
        LoadTechnicians();
        LoadUsers();
        LoadAnalytics();
        LoadReports();
        LoadSettings();
    }

    private void LoadDashboard()
    {
        var stats = ReportService.GetDashboardStats();
        OpenCountText.Text = stats.OpenTickets.ToString();
        InProgressCountText.Text = stats.InProgressTickets.ToString();
        CriticalCountText.Text = stats.CriticalTickets.ToString();
        ResolvedCountText.Text = stats.ResolvedTickets.ToString();

        var allTickets = TicketService.GetTickets();
        var today = DateTime.Today;

        RecentTicketsGrid.ItemsSource = allTickets.Take(12).ToList();
        TechnicianListBox.ItemsSource = ReportService.GetTechnicianSummaries();

        // Dashboard activity is a summary only. We show one latest event here,
        // while the full history is available from the View Full Activity Log button.
        ActivityCreatedTodayText.Text = allTickets.Count(t => t.CreatedDate.Date == today).ToString();
        ActivityResolvedTodayText.Text = allTickets.Count(t => t.ResolvedDate.HasValue && t.ResolvedDate.Value.Date == today).ToString();

        var recentNotifications = TicketService.GetNotifications(50);
        ActivityStatusChangesText.Text = recentNotifications
            .Count(n => n.CreatedDate.Date == today && n.Title.Contains("Status", StringComparison.OrdinalIgnoreCase))
            .ToString();

        var latestActivity = recentNotifications.FirstOrDefault();
        if (latestActivity == null)
        {
            LastActivityTitleText.Text = "No activity yet";
            LastActivityMessageText.Text = "Activities will appear when tickets, users, or technicians change.";
            LastActivityTimeText.Text = string.Empty;
        }
        else
        {
            LastActivityTitleText.Text = latestActivity.Title;
            LastActivityMessageText.Text = latestActivity.Message;
            LastActivityTimeText.Text = latestActivity.CreatedDate.ToString("g");
        }
    }

    private void LoadTickets()
    {
        TicketsGrid.ItemsSource = TicketService.GetTickets(TicketSearchBox.Text);
    }

    private void LoadTechnicians()
    {
        // Refreshes the technician management table.
        // The selected technician panel below is also updated when a row is selected.
        var technicians = ReportService.GetTechnicianSummaries();
        TechniciansGrid.ItemsSource = technicians;

        int total = technicians.Count;
        int available = technicians.Count(t => t.AvailabilityStatus == "Available");
        int onBreak = technicians.Count(t => t.AvailabilityStatus == "On Break");
        int offline = technicians.Count(t => t.AvailabilityStatus == "Offline");
        TechnicianSummaryText.Text = $"Technicians: {total} | Available: {available} | On Break: {onBreak} | Offline: {offline}";

        if (TechniciansGrid.Items.Count > 0 && TechniciansGrid.SelectedItem == null)
        {
            TechniciansGrid.SelectedIndex = 0;
        }
    }

    private void LoadUsers()
    {
        // Users page is for system login accounts, not technician workload.
        var users = AuthService.GetUsers();
        UsersGrid.ItemsSource = users;

        int total = users.Count;
        int admins = users.Count(u => u.Role == "Admin");
        int managers = users.Count(u => u.Role == "Manager");
        int technicians = users.Count(u => u.Role == "Technician");
        int employees = users.Count(u => u.Role == "Employee");
        UsersSummaryText.Text = $"Users: {total} | Admins: {admins} | Managers: {managers} | Technicians: {technicians} | Employees: {employees}";
    }

    private void LoadAnalytics()
    {
        // Analytics is for live insight: counts, visual bars and technician workload.
        var tickets = TicketService.GetTickets();
        AnalyticsTotalTicketsText.Text = tickets.Count.ToString();
        AnalyticsResolvedText.Text = tickets.Count(t => t.Status is "Resolved" or "Closed").ToString();
        AnalyticsCriticalText.Text = tickets.Count(t => t.Priority == "Critical" && t.Status is not "Closed").ToString();
        AnalyticsBreachedText.Text = tickets.Count(t => t.SlaStatus == "Breached").ToString();

        AnalyticsPriorityGrid.ItemsSource = ReportService.AddBars(ReportService.TicketsByPriority());
        AnalyticsDepartmentGrid.ItemsSource = ReportService.AddBars(ReportService.TicketsByDepartment());
        AnalyticsTechnicianGrid.ItemsSource = ReportService.AddBars(ReportService.TechnicianWorkload());
    }

    private void LoadReports()
    {
        // Reports are exportable summaries for management.
        // Monthly is the default report because it is the most common business report.
        GenerateReport("Monthly");
    }

    private void LoadSettings()
    {
        var company = SessionService.CurrentCompany;
        SettingsCompanyNameBox.Text = company?.CompanyName ?? "";
        SettingsCompanyEmailBox.Text = company?.Email ?? "";
        SettingsCompanyPhoneBox.Text = company?.Phone ?? "";
        SettingsCompanyAddressBox.Text = company?.Address ?? "";
        SettingsMessage.Text = "";

        var departments = TicketService.GetDepartments();
        var categories = TicketService.GetCategories();
        var technicians = AuthService.GetUsers("Technician");
        var tickets = TicketService.GetTickets();

        DepartmentsListBox.ItemsSource = departments;
        CategoriesListBox.ItemsSource = categories;
        DepartmentSettingsMessage.Text = "";
        CategorySettingsMessage.Text = "";

        // SLA Management is loaded with professional default rows instead of showing an empty table.
        SlaRulesGrid.ItemsSource = DatabaseService.GetSlaRules();
        SlaRulesGrid.IsReadOnly = false;
        SlaSettingsMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        SlaSettingsMessage.Text = "Edit the hours and click Save SLA Changes.";

    }

    private void ShowOnly(UIElement panel)
    {
        // This hides all pages and then shows only the page the user clicked.
        DashboardPanel.Visibility = Visibility.Collapsed;
        TicketsPanel.Visibility = Visibility.Collapsed;
        CreateTicketPanel.Visibility = Visibility.Collapsed;
        TechniciansPanel.Visibility = Visibility.Collapsed;
        UsersPanel.Visibility = Visibility.Collapsed;
        AnalyticsPanel.Visibility = Visibility.Collapsed;
        ReportsPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        panel.Visibility = Visibility.Visible;
    }

    private void ShowDashboard_Click(object sender, RoutedEventArgs e) { LoadAllData(); ShowOnly(DashboardPanel); }
    private void ShowTickets_Click(object sender, RoutedEventArgs e) { LoadTickets(); ShowOnly(TicketsPanel); }
    private void ShowCreateTicket_Click(object sender, RoutedEventArgs e) { ShowOnly(CreateTicketPanel); }
    private void ShowTechnicians_Click(object sender, RoutedEventArgs e)
    {
        LoadTechnicians();
        ShowOnly(TechniciansPanel);
    }
    private void ShowUsers_Click(object sender, RoutedEventArgs e)
    {
        LoadUsers();
        ShowOnly(UsersPanel);
    }

    private void ShowAnalytics_Click(object sender, RoutedEventArgs e) { LoadAnalytics(); ShowOnly(AnalyticsPanel); }
    private void ShowReports_Click(object sender, RoutedEventArgs e) { LoadReports(); ShowOnly(ReportsPanel); }
    private void ShowSettings_Click(object sender, RoutedEventArgs e) { LoadSettings(); ShowOnly(SettingsPanel); }

    private void ViewFullActivityLog_Click(object sender, RoutedEventArgs e)
    {
        // Opens the full audit/activity history in a separate window so the dashboard stays clean.
        var window = new ActivityLogWindow
        {
            Owner = this
        };
        window.ShowDialog();
    }


    private void ShowFilteredTickets(string filterName, Func<Ticket, bool> filter)
    {
        // Dashboard cards are now working shortcuts.
        // When the admin clicks a card, the Tickets page opens with only that group of tickets.
        TicketSearchBox.Text = string.Empty;
        TicketsGrid.ItemsSource = TicketService.GetTickets()
            .Where(filter)
            .OrderByDescending(t => t.CreatedDate)
            .ToList();
        ShowOnly(TicketsPanel);
    }

    private void OpenTicketsCard_Click(object sender, MouseButtonEventArgs e)
    {
        ShowFilteredTickets("Open Tickets", t => t.Status is "Open" or "Assigned");
    }

    private void InProgressTicketsCard_Click(object sender, MouseButtonEventArgs e)
    {
        ShowFilteredTickets("In Progress", t => t.Status == "In Progress");
    }

    private void CriticalTicketsCard_Click(object sender, MouseButtonEventArgs e)
    {
        ShowFilteredTickets("Critical", t => t.Priority == "Critical" && t.Status is not "Closed");
    }

    private void ResolvedTicketsCard_Click(object sender, MouseButtonEventArgs e)
    {
        ShowFilteredTickets("Resolved", t => t.Status is "Resolved" or "Closed");
    }


    private void TicketSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        LoadTickets();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadAllData();
    }

    private void CreateTicketShortcut_Click(object sender, RoutedEventArgs e)
    {
        ShowOnly(CreateTicketPanel);
    }

    private void SubmitTicket_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TicketTitleBox.Text) || string.IsNullOrWhiteSpace(TicketDescriptionBox.Text))
        {
            CreateTicketMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            CreateTicketMessage.Text = "Please enter a title and description.";
            return;
        }

        int? assignedTo = AssignTechBox.SelectedValue is int id ? id : null;

        var ticket = new Ticket
        {
            Title = TicketTitleBox.Text.Trim(),
            Description = TicketDescriptionBox.Text.Trim(),
            Category = CategoryBox.Text,
            Department = DepartmentBox.Text,
            Priority = PriorityBox.Text,
            CreatedByUserId = SessionService.CurrentUser?.UserId ?? 1,
            AssignedToUserId = assignedTo
        };

        TicketService.CreateTicket(ticket);
        CreateTicketMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        CreateTicketMessage.Text = "Ticket submitted successfully.";

        TicketTitleBox.Clear();
        TicketDescriptionBox.Clear();
        LoadAllData();
        ShowOnly(DashboardPanel);
    }

    private void AddTechnician_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TechNameBox.Text) || string.IsNullOrWhiteSpace(TechUsernameBox.Text) || string.IsNullOrWhiteSpace(TechPasswordBox.Password))
        {
            TechMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            TechMessage.Text = "Please enter name, username and password.";
            return;
        }

        if (!ValidationService.IsValidEmail(TechEmailBox.Text))
        {
            TechMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            TechMessage.Text = ValidationService.EmailError("Technician email");
            return;
        }

        if (!ValidationService.IsValidPhone(TechPhoneBox.Text))
        {
            TechMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            TechMessage.Text = ValidationService.PhoneError("Technician phone number");
            return;
        }

        try
        {
            AuthService.AddUser(new User
            {
                FullName = TechNameBox.Text.Trim(),
                Username = TechUsernameBox.Text.Trim(),
                Email = TechEmailBox.Text.Trim(),
                Phone = TechPhoneBox.Text.Trim(),
                Department = TechDepartmentBox.Text,
                Role = "Technician",
                // New technicians always start as Available.
                // Admin can change them to On Break or Offline later from the status section.
                AvailabilityStatus = "Available"
            }, TechPasswordBox.Password);

            TechMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
            TechMessage.Text = "Technician added successfully and set to Available.";
            TechNameBox.Clear();
            TechUsernameBox.Clear();
            TechEmailBox.Clear();
            TechPhoneBox.Clear();
            TechPasswordBox.Clear();
            LoadAllData();
        }
        catch (Exception ex)
        {
            TechMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            TechMessage.Text = "Could not add technician: " + ex.Message;
        }
    }



    private void AddSystemUser_Click(object sender, RoutedEventArgs e)
    {
        // Adds a login account. This is separate from technician workload.
        if (string.IsNullOrWhiteSpace(SystemUserFullNameBox.Text) ||
            string.IsNullOrWhiteSpace(SystemUserUsernameBox.Text) ||
            string.IsNullOrWhiteSpace(SystemUserPasswordBox.Password))
        {
            UserAccountMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            UserAccountMessage.Text = "Please enter full name, username and temporary password.";
            return;
        }

        string role = "Employee";
        if (SystemUserRoleBox.SelectedItem is ComboBoxItem selectedRole && selectedRole.Content != null)
        {
            role = selectedRole.Content.ToString() ?? "Employee";
        }

        if (!string.IsNullOrWhiteSpace(SystemUserEmailBox.Text) && !ValidationService.IsValidEmail(SystemUserEmailBox.Text))
        {
            UserAccountMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            UserAccountMessage.Text = ValidationService.EmailError("User email");
            return;
        }

        try
        {
            AuthService.AddUser(new User
            {
                FullName = SystemUserFullNameBox.Text.Trim(),
                Username = SystemUserUsernameBox.Text.Trim(),
                Email = SystemUserEmailBox.Text.Trim(),
                Role = role,
                Department = role == "Technician" ? "IT Support" : "",
                AvailabilityStatus = "Available"
            }, SystemUserPasswordBox.Password);

            UserAccountMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
            UserAccountMessage.Text = "User account created successfully.";
            SystemUserFullNameBox.Clear();
            SystemUserUsernameBox.Clear();
            SystemUserEmailBox.Clear();
            SystemUserPasswordBox.Clear();
            SystemUserRoleBox.SelectedIndex = 2;
            LoadAllData();
            ShowOnly(UsersPanel);
        }
        catch (Exception ex)
        {
            UserAccountMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            UserAccountMessage.Text = "Could not create user: " + ex.Message;
        }
    }

    private void DisableSystemUser_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not User user)
        {
            return;
        }

        if (user.UserId == SessionService.CurrentUser?.UserId)
        {
            UserAccountMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            UserAccountMessage.Text = "You cannot disable the account you are currently using.";
            return;
        }

        if (MessageBox.Show($"Disable {user.FullName}'s login account?", "Disable User", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        AuthService.DisableUser(user.UserId);
        UserAccountMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        UserAccountMessage.Text = "User account disabled successfully.";
        LoadAllData();
        ShowOnly(UsersPanel);
    }

    private void ResetSystemUserPassword_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not User user)
        {
            return;
        }

        string temporaryPassword = "Password123";
        AuthService.ResetPassword(user.UserId, temporaryPassword);
        UserAccountMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        UserAccountMessage.Text = $"Temporary password for {user.FullName}: {temporaryPassword}";
        LoadUsers();
    }


    private void EditTechnician_Click(object sender, RoutedEventArgs e)
    {
        // Opens a clean pop-up window for full technician maintenance.
        // This keeps the main Technician page professional and uncluttered.
        if ((sender as FrameworkElement)?.DataContext is not TechnicianSummary technician)
        {
            return;
        }

        TechniciansGrid.SelectedItem = technician;
        TechniciansGrid.ScrollIntoView(technician);

        var editWindow = new TechnicianEditWindow(technician)
        {
            Owner = this
        };

        bool? result = editWindow.ShowDialog();
        if (result == true)
        {
            int technicianId = technician.UserId;
            LoadAllData();
            ShowOnly(TechniciansPanel);

            if (!editWindow.WasDeleted)
            {
                SelectTechnicianById(technicianId);
            }
            else
            {
                ClearTechnicianEditor();
            }
        }
    }

    private void TechniciansGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Selecting a row shows the clean profile summary.
        // Status can be changed directly from this profile section.
        if (TechniciansGrid.SelectedItem is TechnicianSummary tech)
        {
            ShowTechnicianProfile(tech);
        }
    }

    private void ShowTechnicianProfile(TechnicianSummary tech)
    {
        SelectedTechInitialsText.Text = tech.Initials;
        SelectedTechNameText.Text = tech.DisplayName;
        SelectedTechRoleText.Text = "Technician";
        SelectedTechUsernameText.Text = string.IsNullOrWhiteSpace(tech.Username) ? "-" : tech.Username;
        SelectedTechEmailText.Text = string.IsNullOrWhiteSpace(tech.Email) ? "No email added" : tech.Email;
        SelectedTechPhoneText.Text = string.IsNullOrWhiteSpace(tech.Phone) ? "No phone number added" : tech.Phone;
        SelectedTechStatusText.Text = string.IsNullOrWhiteSpace(tech.AvailabilityStatus) ? "Available" : tech.AvailabilityStatus;
        SelectedTechStatusBadge.Background = GetAvailabilityBrush(SelectedTechStatusText.Text);
        QuickTechStatusBox.SelectedItem = SelectedTechStatusText.Text;
        QuickStatusMessage.Text = "";
    }

    private void QuickUpdateTechnicianStatus_Click(object sender, RoutedEventArgs e)
    {
        // Fast daily action: update only technician availability.
        if (TechniciansGrid.SelectedItem is not TechnicianSummary selectedTechnician)
        {
            QuickStatusMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            QuickStatusMessage.Text = "Please select a technician first.";
            return;
        }

        string newStatus = QuickTechStatusBox.Text;
        if (string.IsNullOrWhiteSpace(newStatus))
        {
            newStatus = "Available";
        }

        AuthService.UpdateTechnicianAvailability(selectedTechnician.UserId, newStatus.Trim());

        int updatedId = selectedTechnician.UserId;
        LoadAllData();
        ShowOnly(TechniciansPanel);
        SelectTechnicianById(updatedId);

        QuickStatusMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        QuickStatusMessage.Text = "Technician status updated successfully.";
    }

    private void SelectTechnicianById(int userId)
    {
        // Keeps the same technician selected after saving or updating status.
        foreach (var item in TechniciansGrid.Items)
        {
            if (item is TechnicianSummary tech && tech.UserId == userId)
            {
                TechniciansGrid.SelectedItem = item;
                TechniciansGrid.ScrollIntoView(item);
                break;
            }
        }
    }

    private void ClearTechnicianEditor()
    {
        SelectedTechInitialsText.Text = "--";
        SelectedTechNameText.Text = "Choose a technician";
        SelectedTechRoleText.Text = "Select a technician to view details and update status.";
        SelectedTechUsernameText.Text = "-";
        SelectedTechEmailText.Text = "-";
        SelectedTechPhoneText.Text = "-";
        SelectedTechStatusText.Text = "Available";
        SelectedTechStatusBadge.Background = GetAvailabilityBrush("Available");
        QuickTechStatusBox.SelectedItem = "Available";
        QuickStatusMessage.Text = "";
    }


    private Brush GetAvailabilityBrush(string status)
    {
        // Gives the status badge a business-friendly colour.
        return status switch
        {
            "Available" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
            "On Break" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
            "Offline" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
            _ => new SolidColorBrush(Color.FromRgb(34, 197, 94))
        };
    }

    private void RecentTicketsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-clicking a recent ticket opens its full details.
        if (RecentTicketsGrid.SelectedItem is Ticket ticket)
        {
            OpenTicketDetails(ticket.TicketId);
        }
    }

    private void TicketsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Double-clicking any ticket in the ticket page opens its details.
        if (TicketsGrid.SelectedItem is Ticket ticket)
        {
            OpenTicketDetails(ticket.TicketId);
        }
    }

    private void ViewTicket_Click(object sender, RoutedEventArgs e)
    {
        // View button inside a ticket row opens the same ticket detail window.
        if ((sender as FrameworkElement)?.DataContext is Ticket ticket)
        {
            OpenTicketDetails(ticket.TicketId);
        }
    }

    private void CloseTicket_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is Ticket ticket)
        {
            TicketService.UpdateStatus(ticket.TicketId, "Closed");
            LoadAllData();
        }
    }

    private void OpenTicketDetails(int ticketId)
    {
        var details = new TicketDetailsWindow(ticketId) { Owner = this };
        details.ShowDialog();
        LoadAllData();
    }


    private void GenerateReport(string reportType)
    {
        // This method builds a clear text report and a small data table.
        // The same data can then be exported to CSV or saved as a text report.
        var tickets = TicketService.GetTickets();
        var company = SessionService.CurrentCompany;
        DateTime now = DateTime.Now;

        _currentReportTitle = reportType + " Service Desk Report";

        _currentReportRows = reportType switch
        {
            "Technician" => ReportService.TechnicianWorkload(),
            "Department" => ReportService.TicketsByDepartment(),
            "SLA" => ReportService.SlaSummary(),
            _ => ReportService.TicketsByStatus()
        };

        int totalTickets = tickets.Count;
        int openTickets = tickets.Count(t => t.Status is "Open" or "Assigned");
        int inProgressTickets = tickets.Count(t => t.Status == "In Progress");
        int resolvedTickets = tickets.Count(t => t.Status is "Resolved" or "Closed");
        int criticalTickets = tickets.Count(t => t.Priority == "Critical" && t.Status is not "Closed");
        int breachedTickets = tickets.Count(t => t.SlaStatus == "Breached");

        var builder = new StringBuilder();
        builder.AppendLine(_currentReportTitle);
        builder.AppendLine(new string('-', 42));
        builder.AppendLine($"Company: {company?.CompanyName ?? "ServiceDesk Pro"}");
        builder.AppendLine($"Generated: {now:dd MMMM yyyy HH:mm}");
        builder.AppendLine($"Generated By: {SessionService.CurrentUser?.FullName ?? "Admin"}");
        builder.AppendLine();
        builder.AppendLine("Summary");
        builder.AppendLine($"Total Tickets: {totalTickets}");
        builder.AppendLine($"Open Tickets: {openTickets}");
        builder.AppendLine($"In Progress: {inProgressTickets}");
        builder.AppendLine($"Resolved / Closed: {resolvedTickets}");
        builder.AppendLine($"Critical Active: {criticalTickets}");
        builder.AppendLine($"SLA Breached: {breachedTickets}");
        builder.AppendLine();

        if (reportType == "Daily")
        {
            var today = tickets.Where(t => t.CreatedDate.Date == now.Date).ToList();
            builder.AppendLine("Daily Ticket Activity");
            builder.AppendLine($"Tickets Created Today: {today.Count}");
            foreach (var ticket in today.Take(15))
                builder.AppendLine($"- {ticket.TicketNumber}: {ticket.Title} ({ticket.Priority}, {ticket.Status})");
        }
        else if (reportType == "Weekly")
        {
            var start = now.Date.AddDays(-7);
            var weekTickets = tickets.Where(t => t.CreatedDate >= start).ToList();
            builder.AppendLine("Weekly Ticket Activity");
            builder.AppendLine($"Tickets Created Last 7 Days: {weekTickets.Count}");
            foreach (var ticket in weekTickets.Take(15))
                builder.AppendLine($"- {ticket.TicketNumber}: {ticket.Title} ({ticket.Department})");
        }
        else if (reportType == "Monthly")
        {
            var monthTickets = tickets.Where(t => t.CreatedDate.Month == now.Month && t.CreatedDate.Year == now.Year).ToList();
            builder.AppendLine("Monthly Ticket Activity");
            builder.AppendLine($"Tickets Created This Month: {monthTickets.Count}");
            builder.AppendLine($"Most Active Department: {ReportService.TicketsByDepartment().FirstOrDefault()?.Label ?? "N/A"}");
            builder.AppendLine($"Top Technician Workload: {ReportService.TechnicianWorkload().FirstOrDefault()?.Label ?? "N/A"}");
        }
        else if (reportType == "Technician")
        {
            builder.AppendLine("Technician Performance");
            foreach (var tech in ReportService.GetTechnicianSummaries())
                builder.AppendLine($"- {tech.FullName}: {tech.AssignedTickets} active, {tech.ResolvedTickets} resolved, {tech.AvailabilityStatus}");
        }
        else if (reportType == "Department")
        {
            builder.AppendLine("Department Ticket Volume");
            foreach (var row in ReportService.TicketsByDepartment())
                builder.AppendLine($"- {row.Label}: {row.Count}");
        }
        else if (reportType == "SLA")
        {
            builder.AppendLine("SLA Summary");
            foreach (var row in ReportService.SlaSummary())
                builder.AppendLine($"- {row.Label}: {row.Count}");
        }

        _currentReportText = builder.ToString();
        ReportPreviewTitleText.Text = _currentReportTitle;
        ReportPreviewBox.Text = _currentReportText;
        ReportDataGrid.ItemsSource = _currentReportRows;
        ReportMessageText.Text = reportType + " report generated. You can export it to CSV or save it as text.";
    }

    private void DailyReport_Click(object sender, RoutedEventArgs e) => GenerateReport("Daily");
    private void WeeklyReport_Click(object sender, RoutedEventArgs e) => GenerateReport("Weekly");
    private void MonthlyReport_Click(object sender, RoutedEventArgs e) => GenerateReport("Monthly");
    private void TechnicianReport_Click(object sender, RoutedEventArgs e) => GenerateReport("Technician");
    private void DepartmentReport_Click(object sender, RoutedEventArgs e) => GenerateReport("Department");
    private void SlaReport_Click(object sender, RoutedEventArgs e) => GenerateReport("SLA");

    private void ExportReportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_currentReportRows.Count == 0) GenerateReport("Monthly");

        var dialog = new SaveFileDialog
        {
            FileName = _currentReportTitle.Replace(" ", "_") + ".csv",
            Filter = "CSV files (*.csv)|*.csv"
        };

        if (dialog.ShowDialog() == true)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Label,Count");
            foreach (var row in _currentReportRows)
                csv.AppendLine($"\"{row.Label.Replace("\"", "\"\"")}\",{row.Count}");

            File.WriteAllText(dialog.FileName, csv.ToString());
            ReportMessageText.Text = "CSV report exported successfully.";
        }
    }

    private void SaveReportText_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_currentReportText)) GenerateReport("Monthly");

        var dialog = new SaveFileDialog
        {
            FileName = _currentReportTitle.Replace(" ", "_") + ".txt",
            Filter = "Text report (*.txt)|*.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            File.WriteAllText(dialog.FileName, _currentReportText);
            ReportMessageText.Text = "Text report saved successfully.";
        }
    }


    private void SaveCompanySettings_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SettingsCompanyNameBox.Text) || string.IsNullOrWhiteSpace(SettingsCompanyEmailBox.Text))
        {
            SettingsMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            SettingsMessage.Text = "Please enter at least the company name and email.";
            return;
        }

        if (!ValidationService.IsValidEmail(SettingsCompanyEmailBox.Text))
        {
            SettingsMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            SettingsMessage.Text = ValidationService.EmailError("Company email");
            return;
        }

        if (!ValidationService.IsValidPhone(SettingsCompanyPhoneBox.Text))
        {
            SettingsMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            SettingsMessage.Text = ValidationService.PhoneError("Company phone number");
            return;
        }

        var company = SessionService.CurrentCompany;
        if (company == null)
        {
            SettingsMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            SettingsMessage.Text = "Company profile could not be found.";
            return;
        }

        company.CompanyName = SettingsCompanyNameBox.Text.Trim();
        company.Email = SettingsCompanyEmailBox.Text.Trim();
        company.Phone = SettingsCompanyPhoneBox.Text.Trim();
        company.Address = SettingsCompanyAddressBox.Text.Trim();

        DatabaseService.UpdateCompany(company);
        SessionService.CurrentCompany = company;
        LoadAllData();
        ShowOnly(SettingsPanel);
        SettingsMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        SettingsMessage.Text = "Company details saved successfully.";
    }

    private void AddDepartment_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewDepartmentBox.Text))
        {
            DepartmentSettingsMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            DepartmentSettingsMessage.Text = "Enter a department name first.";
            return;
        }

        TicketService.AddDepartment(NewDepartmentBox.Text);
        NewDepartmentBox.Clear();
        LoadAllData();
        ShowOnly(SettingsPanel);
        DepartmentSettingsMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        DepartmentSettingsMessage.Text = "Department added. It is now available in tickets and technicians.";
    }

    private void DeleteDepartment_Click(object sender, RoutedEventArgs e)
    {
        if (DepartmentsListBox.SelectedItem is not string department)
        {
            DepartmentSettingsMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            DepartmentSettingsMessage.Text = "Select a department to remove.";
            return;
        }

        TicketService.DeleteDepartment(department);
        LoadAllData();
        ShowOnly(SettingsPanel);
        DepartmentSettingsMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        DepartmentSettingsMessage.Text = "Department removed from the selectable list.";
    }

    private void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewCategoryBox.Text))
        {
            CategorySettingsMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            CategorySettingsMessage.Text = "Enter a category name first.";
            return;
        }

        TicketService.AddCategory(NewCategoryBox.Text);
        NewCategoryBox.Clear();
        LoadAllData();
        ShowOnly(SettingsPanel);
        CategorySettingsMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        CategorySettingsMessage.Text = "Category added. It is now available when creating tickets.";
    }

    private void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        if (CategoriesListBox.SelectedItem is not string category)
        {
            CategorySettingsMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            CategorySettingsMessage.Text = "Select a category to remove.";
            return;
        }

        TicketService.DeleteCategory(category);
        LoadAllData();
        ShowOnly(SettingsPanel);
        CategorySettingsMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        CategorySettingsMessage.Text = "Category removed from the selectable list.";
    }

    private void EditSla_Click(object sender, RoutedEventArgs e)
    {
        // Admins press Edit first so SLA values are not changed by mistake.
        SlaRulesGrid.IsReadOnly = false;
        SlaSettingsMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        SlaSettingsMessage.Text = "SLA rules are editable now. Change the hours and click Save Changes.";
    }

    private void SaveSlaRules_Click(object sender, RoutedEventArgs e)
    {
        // Save the edited SLA values to the database.
        // These values are then used automatically when new tickets are created.
        // CommitEdit makes sure the value currently typed in a TextBox is saved before reading the rows.
        SlaRulesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        SlaRulesGrid.CommitEdit(DataGridEditingUnit.Row, true);

        var rules = SlaRulesGrid.ItemsSource as IEnumerable<SlaRule>;
        if (rules == null)
        {
            SlaSettingsMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
            SlaSettingsMessage.Text = "Could not read SLA rules.";
            return;
        }

        foreach (var rule in rules)
        {
            if (rule.ResponseHours <= 0 || rule.ResolutionHours <= 0)
            {
                SlaSettingsMessage.Foreground = System.Windows.Media.Brushes.IndianRed;
                SlaSettingsMessage.Text = "Response and resolution hours must be greater than zero.";
                return;
            }
        }

        DatabaseService.SaveSlaRules(rules);
        SlaRulesGrid.ItemsSource = DatabaseService.GetSlaRules();
        SlaRulesGrid.IsReadOnly = false;
        SlaSettingsMessage.Foreground = System.Windows.Media.Brushes.LightGreen;
        SlaSettingsMessage.Text = "SLA rules saved successfully. New tickets will use the updated targets.";
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        SessionService.Clear();
        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        Close();
    }
}
