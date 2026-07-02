using System.Windows;
using System.Windows.Threading;

namespace DeltaApp.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Nunca falhar em silêncio: mostra o erro em vez de fechar sem aviso.
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "Ocorreu um erro:\n\n" + args.Exception.Message,
                "Delta Decisão", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show(
                "Erro inesperado:\n\n" + (ex?.Message ?? "desconhecido"),
                "Delta Decisão", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        Config.Load();
        Settings.Load();
    }
}
