using System.Windows;
using System.Windows.Input;
using ServiceDeskPro.Models;
using ServiceDeskPro.Services;

namespace ServiceDeskPro.Views;

public partial class ForgotPasswordWindow : Window
{
    private User? _verifiedUser;

    public ForgotPasswordWindow()
    {
        InitializeComponent();
        UsernameBox.Focus();
    }

    private void Verify_Click(object sender, RoutedEventArgs e)
    {
        VerifyAccount();
    }

    private void VerifyAccount()
    {
        VerifyMessageText.Text = string.Empty;

        if (string.IsNullOrWhiteSpace(UsernameBox.Text) || string.IsNullOrWhiteSpace(EmailBox.Text))
        {
            VerifyMessageText.Text = "Please enter both username and email address.";
            return;
        }

        if (!ValidationService.IsValidEmail(EmailBox.Text))
        {
            VerifyMessageText.Text = ValidationService.EmailError("Email address");
            return;
        }

        _verifiedUser = AuthService.FindActiveUserByUsernameAndEmail(UsernameBox.Text, EmailBox.Text);
        if (_verifiedUser == null)
        {
            VerifyMessageText.Text = "No active account was found with that username and email.";
            return;
        }

        VerifiedUserText.Text = $"Verified account: {_verifiedUser.FullName} ({_verifiedUser.Username})";
        VerifyPanel.Visibility = Visibility.Collapsed;
        ResetPanel.Visibility = Visibility.Visible;
        NewPasswordBox.Focus();
    }

    private void SavePassword_Click(object sender, RoutedEventArgs e)
    {
        SaveNewPassword();
    }

    private void ConfirmPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Pressing Enter saves the new password, just like many real login systems.
        if (e.Key == Key.Enter) SaveNewPassword();
    }

    private void SaveNewPassword()
    {
        ResetMessageText.Foreground = System.Windows.Media.Brushes.IndianRed;
        ResetMessageText.Text = string.Empty;

        if (_verifiedUser == null)
        {
            ResetMessageText.Text = "Please verify your account first.";
            return;
        }

        if (NewPasswordBox.Password.Length < 6)
        {
            ResetMessageText.Text = "Password must be at least 6 characters long.";
            return;
        }

        if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
        {
            ResetMessageText.Text = "Passwords do not match.";
            return;
        }

        AuthService.ResetPasswordByForgotPassword(_verifiedUser.UserId, NewPasswordBox.Password);

        MessageBox.Show("Password updated successfully. You can now login with your new password.",
            "Password Updated", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        ResetPanel.Visibility = Visibility.Collapsed;
        VerifyPanel.Visibility = Visibility.Visible;
        NewPasswordBox.Clear();
        ConfirmPasswordBox.Clear();
        ResetMessageText.Text = string.Empty;
        UsernameBox.Focus();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
