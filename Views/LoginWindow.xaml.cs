using System.Windows;
using System.Windows.Input;
using ServiceDeskPro.Services;

namespace ServiceDeskPro.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        UsernameBox.Focus();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        LoginUser();
    }

    private void ForgotPassword_Click(object sender, RoutedEventArgs e)
    {
        var forgotWindow = new ForgotPasswordWindow
        {
            Owner = this
        };
        forgotWindow.ShowDialog();
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Pressing Enter is a small professional touch that users expect on login screens.
        if (e.Key == Key.Enter) LoginUser();
    }

    private void LoginUser()
    {
        var user = AuthService.Login(UsernameBox.Text, PasswordBox.Password);
        if (user == null)
        {
            MessageText.Text = "Invalid username or password.";
            return;
        }

        // Login is normally the main window at this point.
        // Set the dashboard as the new main window before closing login,
        // so the application continues running after a successful login.
        var dashboard = new DashboardWindow();
        Application.Current.MainWindow = dashboard;
        dashboard.Show();
        Close();
    }
}
