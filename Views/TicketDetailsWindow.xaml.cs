using System.Windows;
using ServiceDeskPro.Models;
using ServiceDeskPro.Services;

namespace ServiceDeskPro.Views;

public partial class TicketDetailsWindow : Window
{
    private readonly int _ticketId;
    private Ticket? _ticket;

    public TicketDetailsWindow(int ticketId)
    {
        InitializeComponent();
        _ticketId = ticketId;
        LoadWindowData();
    }

    private void LoadWindowData()
    {
        // The ticket is loaded from the database every time so the screen stays up to date.
        _ticket = TicketService.GetTickets().FirstOrDefault(t => t.TicketId == _ticketId);
        if (_ticket == null)
        {
            MessageBox.Show("Ticket could not be found.");
            Close();
            return;
        }

        TicketNumberText.Text = _ticket.TicketNumber;
        TitleText.Text = _ticket.Title;
        DescriptionText.Text = _ticket.Description;
        PriorityText.Text = _ticket.Priority;
        StatusText.Text = _ticket.Status;
        DepartmentText.Text = _ticket.Department;
        AssignedText.Text = _ticket.AssignedToName;
        CreatedByText.Text = _ticket.CreatedByName;
        SlaText.Text = _ticket.SlaStatus;

        TechnicianBox.ItemsSource = AuthService.GetUsers("Technician");
        TechnicianBox.SelectedValue = _ticket.AssignedToUserId;
        StatusBox.ItemsSource = new[] { "Open", "Assigned", "In Progress", "Waiting User", "Resolved", "Closed" };
        StatusBox.SelectedItem = _ticket.Status;

        CommentsListBox.ItemsSource = TicketService.GetComments(_ticketId)
            .Select(c => $"{c.UserName} - {c.CreatedDate:g}\n{c.CommentText}")
            .ToList();
    }

    private void Assign_Click(object sender, RoutedEventArgs e)
    {
        if (TechnicianBox.SelectedValue is not int technicianId)
        {
            MessageBox.Show("Please select a technician.");
            return;
        }

        TicketService.AssignTicket(_ticketId, technicianId);
        LoadWindowData();
    }

    private void UpdateStatus_Click(object sender, RoutedEventArgs e)
    {
        if (StatusBox.SelectedItem is not string status) return;
        TicketService.UpdateStatus(_ticketId, status);
        LoadWindowData();
    }

    private void AddComment_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CommentBox.Text)) return;
        TicketService.AddComment(_ticketId, SessionService.CurrentUser?.UserId ?? 1, CommentBox.Text.Trim());
        CommentBox.Clear();
        LoadWindowData();
    }
}
