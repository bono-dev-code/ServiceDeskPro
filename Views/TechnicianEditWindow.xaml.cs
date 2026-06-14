using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ServiceDeskPro.Models;
using ServiceDeskPro.Services;

namespace ServiceDeskPro.Views;

public partial class TechnicianEditWindow : Window
{
    private readonly TechnicianSummary _technician;

    public bool WasSaved { get; private set; }
    public bool WasDeleted { get; private set; }

    public TechnicianEditWindow(TechnicianSummary technician)
    {
        InitializeComponent();
        _technician = technician;
        LoadWindowData();
    }

    private void LoadWindowData()
    {
        FullNameBox.Text = _technician.DisplayName;
        EmailBox.Text = _technician.Email;
        PhoneBox.Text = _technician.Phone;

        DepartmentBox.ItemsSource = TicketService.GetDepartments();
        DepartmentBox.SelectedItem = _technician.Department;
        if (DepartmentBox.SelectedItem == null)
        {
            DepartmentBox.Text = _technician.DisplayDepartment;
        }

        SelectStatus(_technician.AvailabilityStatus);
    }

    private void SelectStatus(string status)
    {
        string value = string.IsNullOrWhiteSpace(status) ? "Available" : status;
        foreach (ComboBoxItem item in StatusBox.Items)
        {
            if (string.Equals(item.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                StatusBox.SelectedItem = item;
                return;
            }
        }
        StatusBox.SelectedIndex = 0;
    }

    private string SelectedStatus()
    {
        if (StatusBox.SelectedItem is ComboBoxItem item)
        {
            return item.Content?.ToString() ?? "Available";
        }
        return string.IsNullOrWhiteSpace(StatusBox.Text) ? "Available" : StatusBox.Text.Trim();
    }

    private void SaveChanges_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FullNameBox.Text) ||
            string.IsNullOrWhiteSpace(EmailBox.Text) ||
            string.IsNullOrWhiteSpace(PhoneBox.Text) ||
            string.IsNullOrWhiteSpace(DepartmentBox.Text))
        {
            MessageText.Foreground = System.Windows.Media.Brushes.IndianRed;
            MessageText.Text = "Please complete name, email, phone and department.";
            return;
        }

        if (!ValidationService.IsValidEmail(EmailBox.Text))
        {
            MessageText.Foreground = System.Windows.Media.Brushes.IndianRed;
            MessageText.Text = ValidationService.EmailError("Technician email");
            return;
        }

        if (!ValidationService.IsValidPhone(PhoneBox.Text))
        {
            MessageText.Foreground = System.Windows.Media.Brushes.IndianRed;
            MessageText.Text = ValidationService.PhoneError("Technician phone number");
            return;
        }

        var updatedTechnician = new User
        {
            UserId = _technician.UserId,
            FullName = FullNameBox.Text.Trim(),
            Email = EmailBox.Text.Trim(),
            Phone = PhoneBox.Text.Trim(),
            Department = DepartmentBox.Text.Trim(),
            AvailabilityStatus = SelectedStatus()
        };

        AuthService.UpdateTechnician(updatedTechnician);
        WasSaved = true;
        DialogResult = true;
        Close();
    }

    private void DeleteTechnician_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"Delete {_technician.DisplayName}?\n\nOpen assigned tickets will be unassigned and the technician account will be deactivated.",
            "Confirm Delete Technician",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        AuthService.DeleteTechnician(_technician.UserId);
        WasDeleted = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
