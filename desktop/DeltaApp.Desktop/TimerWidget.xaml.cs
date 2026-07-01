using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DeltaApp.Desktop.Models;
using DeltaApp.Desktop.Services;

namespace DeltaApp.Desktop;

/// <summary>Janela flutuante (Topmost) com o cronômetro da solicitação em andamento.</summary>
public partial class TimerWidget : Window
{
    private readonly ApiClient _api;
    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromSeconds(1) };

    private ActiveTimerDto? _active;
    private DateTime _fetchedAt;
    private int _lastSolicitationId;
    private string _lastCode = "";
    private int _tick;

    public TimerWidget(ApiClient api)
    {
        _api = api;
        InitializeComponent();
        PositionBottomRight();
        _ticker.Tick += Tick;
        _ticker.Start();
        Loaded += (_, _) => Sync();
        Closed += (_, _) => _ticker.Stop();
    }

    private void PositionBottomRight()
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - Width - 16;
        Top = wa.Bottom - Height - 16;
    }

    /// <summary>Ressincroniza com o servidor (chamado pela MainWindow após ações).</summary>
    public async void Sync()
    {
        try
        {
            _active = await _api.GetActiveAsync();
            _fetchedAt = DateTime.Now;
            if (_active is not null)
            {
                _lastSolicitationId = _active.SolicitationId;
                _lastCode = _active.Code;
            }
            UpdateDisplay();
        }
        catch { /* silencioso na janelinha */ }
    }

    private void Tick(object? sender, EventArgs e)
    {
        _tick++;
        if (_tick % 5 == 0) Sync();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_active is not null)
        {
            CodeText.Text = _active.Code;
            var seconds = _active.AccumulatedTodayMinutes * 60 + (DateTime.Now - _fetchedAt).TotalSeconds;
            TimeText.Text = FormatHelper.Hms(seconds);
            StatusDot.Visibility = Visibility.Visible;
            PauseBtn.IsEnabled = true;
            ContinueBtn.IsEnabled = false;
        }
        else
        {
            CodeText.Text = _lastSolicitationId > 0 ? _lastCode : "sem SO";
            StatusDot.Visibility = Visibility.Collapsed;
            PauseBtn.IsEnabled = false;
            ContinueBtn.IsEnabled = _lastSolicitationId > 0;
        }
    }

    private void Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private async void Pause_Click(object sender, RoutedEventArgs e)
    {
        try { await _api.PauseAsync(); } catch { }
        Sync();
    }

    private async void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (_lastSolicitationId <= 0) return;
        try
        {
            _active = await _api.StartAsync(_lastSolicitationId);
            _fetchedAt = DateTime.Now;
            UpdateDisplay();
        }
        catch { }
    }

    private async void Finish_Click(object sender, RoutedEventArgs e)
    {
        var id = _active?.SolicitationId ?? _lastSolicitationId;
        if (id <= 0) return;
        try { await _api.FinishAsync(id); } catch { }
        Sync();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
