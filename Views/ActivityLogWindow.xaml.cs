using System.Windows;
using ServiceDeskPro.Services;

namespace ServiceDeskPro.Views;

public partial class ActivityLogWindow : Window
{
    public ActivityLogWindow()
    {
        InitializeComponent();

        // The dashboard shows only the latest 5 activities.
        // This window shows a larger history for admin review.
        ActivityGrid.ItemsSource = TicketService.GetNotifications(200);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
