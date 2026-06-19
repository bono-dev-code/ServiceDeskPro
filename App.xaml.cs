using System.Windows;
using ServiceDeskPro.Services;
using ServiceDeskPro.Views;

namespace ServiceDeskPro;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // This creates the database and all required tables the first time the app runs.
        DatabaseService.InitializeDatabase();

        // A real company system usually starts with setup first, then login afterwards.
        Window startWindow = DatabaseService.CompanyExists()
            ? new LoginWindow()
            : new RegisterCompanyWindow();

        MainWindow = startWindow;
        startWindow.Show();
    }
}
