using System.Windows;
using DeltaApp.Desktop.Services;

namespace DeltaApp.Desktop;

public partial class LoginWindow : Window
{
    private readonly SupabaseAuthService _auth = new();

    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        LoginButton.IsEnabled = false;
        LoginButton.Content = "Entrando…";
        try
        {
            var session = await _auth.SignInAsync(EmailBox.Text.Trim(), PasswordBox.Password);
            Session.Current = session;

            Window next = session.MustChangePassword
                ? new ChangePasswordWindow()
                : new MainWindow();
            next.Show();
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Content = "Entrar";
        }
    }
}
