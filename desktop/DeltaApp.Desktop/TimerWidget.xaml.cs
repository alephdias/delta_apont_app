using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DeltaApp.Desktop.Services;

namespace DeltaApp.Desktop;

/// <summary>Janela flutuante (Topmost) que reflete o mesmo cronômetro da janela principal.</summary>
public partial class TimerWidget : Window
{
    private readonly TimerHub _timer;
    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromSeconds(1) };

    public TimerWidget(TimerHub timer)
    {
        _timer = timer;
        InitializeComponent();
        PositionBottomRight();

        _timer.Changed += UpdateDisplay;
        _ticker.Tick += (_, _) => UpdateDisplay();
        _ticker.Start();

        Loaded += (_, _) => UpdateDisplay();
        Closed += (_, _) =>
        {
            _ticker.Stop();
            _timer.Changed -= UpdateDisplay;
        };
    }

    private void PositionBottomRight()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - 16;
        Top = wa.Bottom - Height - 16;
    }

    private void UpdateDisplay()
    {
        TimeText.Text = FormatHelper.Hms(_timer.ElapsedSeconds());
        if (_timer.IsRunning)
        {
            CodeText.Text = _timer.Active!.Code;
            StatusDot.Visibility = Visibility.Visible;
            PauseBtn.IsEnabled = true;
            ContinueBtn.IsEnabled = false;
        }
        else
        {
            CodeText.Text = _timer.LastSolicitationId > 0 ? _timer.LastCode : "sem SO";
            StatusDot.Visibility = Visibility.Collapsed;
            PauseBtn.IsEnabled = false;
            ContinueBtn.IsEnabled = _timer.LastSolicitationId > 0;
        }
    }

    private void Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private async void Pause_Click(object sender, RoutedEventArgs e)
    {
        try { await _timer.PauseAsync(); } catch { }
    }

    private async void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (_timer.LastSolicitationId <= 0) return;
        try { await _timer.StartAsync(_timer.LastSolicitationId); } catch { }
    }

    private async void Finish_Click(object sender, RoutedEventArgs e)
    {
        var id = _timer.Active?.SolicitationId ?? _timer.LastSolicitationId;
        if (id <= 0) return;
        try { await _timer.FinishAsync(id); } catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
