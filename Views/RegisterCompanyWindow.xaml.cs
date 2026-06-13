using System.Windows;
using ServiceDeskPro.Models;
using ServiceDeskPro.Services;

namespace ServiceDeskPro.Views;

public partial class RegisterCompanyWindow : Window
{
    public RegisterCompanyWindow()
    {
        InitializeComponent();
    }

    private void CreateCompany_Click(object sender, RoutedEventArgs e)
    {
        // Simple validation prevents empty records from being saved to the database.
        if (string.IsNullOrWhiteSpace(CompanyNameBox.Text) ||
            string.IsNullOrWhiteSpace(CompanyEmailBox.Text) ||
            string.IsNullOrWhiteSpace(FullNameBox.Text) ||
            string.IsNullOrWhiteSpace(UsernameBox.Text) ||
            string.IsNullOrWhiteSpace(AdminEmailBox.Text) ||
            string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            MessageText.Text = "Please complete all required fields.";
            return;
        }

        if (!ValidationService.IsValidEmail(CompanyEmailBox.Text))
        {
            MessageText.Text = ValidationService.EmailError("Company email");
            return;
        }

        if (!ValidationService.IsValidPhone(PhoneBox.Text))
        {
            MessageText.Text = ValidationService.PhoneError("Company phone number");
            return;
        }

        if (!ValidationService.IsValidEmail(AdminEmailBox.Text))
        {
            MessageText.Text = ValidationService.EmailError("Admin email");
            return;
        }

        if (PasswordBox.Password != ConfirmPasswordBox.Password)
        {
            MessageText.Text = "Passwords do not match.";
            return;
        }

        if (PasswordBox.Password.Length < 6)
        {
            MessageText.Text = "Password must be at least 6 characters.";
            return;
        }

        try
        {
            var company = new Company
            {
                CompanyName = CompanyNameBox.Text.Trim(),
                Email = CompanyEmailBox.Text.Trim(),
                Phone = PhoneBox.Text.Trim(),
                Address = AddressBox.Text.Trim()
            };

            var admin = new User
            {
                FullName = FullNameBox.Text.Trim(),
                Username = UsernameBox.Text.Trim(),
                Email = AdminEmailBox.Text.Trim(),
                Role = "Admin"
            };

            DatabaseService.RegisterCompanyAndAdmin(company, admin, PasswordBox.Password);
            MessageBox.Show("Company account created successfully. Please login as Admin.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // The register window is the current main window on first launch.
            // We set the login window as the new main window BEFORE closing this one,
            // otherwise WPF may shut down the whole app when the register window closes.
            var login = new LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();
            Close();
        }
        catch (Exception ex)
        {
            MessageText.Text = "Could not create account: " + ex.Message;
        }
    }
}
