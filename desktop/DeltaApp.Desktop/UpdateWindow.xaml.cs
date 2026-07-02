using System.Windows;
using DeltaApp.Desktop.Services;

namespace DeltaApp.Desktop;

public partial class UpdateWindow : Window
{
    public UpdateWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RunAsync();
    }

    private async Task RunAsync()
    {
        try
        {
            var progress = new Progress<double>(p =>
            {
                Bar.Value = p * 100;
                Pct.Text = $"{(int)(p * 100)}%";
            });

            var path = await Updater.DownloadAsync(progress);

            StatusText.Text = "Instalando e reiniciando…";
            Bar.Value = 100;
            Pct.Text = "100%";
            await Task.Delay(400);

            Updater.ApplyAndRestart(path);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Não foi possível atualizar automaticamente:\n\n" + ex.Message +
                "\n\nVocê pode baixar manualmente pela aba Aplicativo.",
                "Atualização", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
        }
    }
}
