using System.Windows;
using DeltaApp.Desktop.Services;

namespace DeltaApp.Desktop;

public partial class LoginWindow : Window
{
    private readonly SupabaseAuthService _auth = new();

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await TrySilentLoginAsync();
    }

    private async System.Threading.Tasks.Task TrySilentLoginAsync()
    {
        var refresh = SessionStore.LoadRefreshToken();
        if (string.IsNullOrEmpty(refresh)) return;

        LoginButton.IsEnabled = false;
        LoginButton.Content = "Entrando…";
        try
        {
            var session = await _auth.RefreshAsync(refresh);
            if (session is null)
            {
                SessionStore.Clear();
                return;
            }
            SessionStore.SaveRefreshToken(session.RefreshToken);
            Session.Current = session;
            OpenNext(session);
        }
        catch
        {
            // segue para o login manual
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Content = "Entrar";
        }
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
            SessionStore.SaveRefreshToken(session.RefreshToken);
            OpenNext(session);
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

    private void OpenNext(AuthSession session)
    {
        Window next = session.MustChangePassword ? new ChangePasswordWindow() : new MainWindow();
        next.Show();
        Close();
    }
}
