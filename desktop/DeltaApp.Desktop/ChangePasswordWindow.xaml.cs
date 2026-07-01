using System.Windows;
using DeltaApp.Desktop.Services;

namespace DeltaApp.Desktop;

public partial class ChangePasswordWindow : Window
{
    private readonly SupabaseAuthService _auth = new();

    public ChangePasswordWindow()
    {
        InitializeComponent();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        var pwd = PasswordBox.Password;
        if (pwd.Length < 6)
        {
            ShowError("A senha deve ter pelo menos 6 caracteres.");
            return;
        }
        if (pwd != ConfirmBox.Password)
        {
            ShowError("As senhas não conferem.");
            return;
        }

        SaveButton.IsEnabled = false;
        SaveButton.Content = "Salvando…";
        try
        {
            await _auth.UpdatePasswordAsync(Session.Current!.AccessToken, pwd);
            Session.Current.MustChangePassword = false;
            new MainWindow().Show();
            Close();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SaveButton.IsEnabled = true;
            SaveButton.Content = "Salvar e continuar";
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        Session.Current = null;
        SessionStore.Clear();
        new LoginWindow().Show();
        Close();
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.Visibility = Visibility.Visible;
    }
}
