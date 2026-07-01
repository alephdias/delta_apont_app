using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DeltaApp.Desktop.Models;
using DeltaApp.Desktop.Services;

namespace DeltaApp.Desktop;

public partial class MainWindow : Window
{
    private readonly ApiClient _api = new();
    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromSeconds(1) };

    private List<ClientDto> _clients = new();
    private List<SolicitationDto> _solicitations = new();

    private ActiveTimerDto? _active;
    private DateTime _activeFetchedAt;
    private int _lastSolicitationId;
    private TimerWidget? _widget;

    public MainWindow()
    {
        InitializeComponent();
        EmailText.Text = Session.Current?.Email ?? "";
        DatePick.SelectedDate = DateTime.Today;
        _ticker.Tick += Ticker_Tick;
        _ticker.Start();
        Loaded += async (_, _) => await RefreshAllAsync();
        Closed += (_, _) => { _ticker.Stop(); _widget?.Close(); };
    }

    private DateOnly SelectedDate =>
        DateOnly.FromDateTime(DatePick.SelectedDate ?? DateTime.Today);

    private async Task RefreshAllAsync()
    {
        try
        {
            _clients = await _api.GetClientsAsync() ?? new();
            ClientCombo.ItemsSource = _clients;
            _solicitations = await _api.GetSolicitationsAsync() ?? new();
            SolGrid.ItemsSource = _solicitations;
            await LoadDayAsync();
            await RefreshActiveAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadDayAsync()
    {
        var entries = await _api.GetDayEntriesAsync(SelectedDate) ?? new();
        DayGrid.ItemsSource = entries;
        var real = entries.Sum(e => e.RealMinutes);
        var adj = entries.Sum(e => e.AdjustedMinutes);
        DayTotals.Text = $"Real {FormatHelper.Minutes(real)}  ·  Apontado {FormatHelper.Minutes(adj)}";
    }

    private async Task RefreshActiveAsync()
    {
        _active = await _api.GetActiveAsync();
        _activeFetchedAt = DateTime.Now;
        if (_active is not null) _lastSolicitationId = _active.SolicitationId;
        UpdateTimerPanel();
    }

    private void UpdateTimerPanel()
    {
        if (_active is null)
        {
            RunningCode.Text = "Nenhum cronômetro ativo";
            ElapsedLabel.Text = "00:00:00";
            PauseButton.IsEnabled = false;
            FinishButton.IsEnabled = _lastSolicitationId > 0;
        }
        else
        {
            var client = string.IsNullOrEmpty(_active.ClientName) ? "" : $"  ·  {_active.ClientName}";
            RunningCode.Text = $"{_active.Code}{client}";
            PauseButton.IsEnabled = true;
            FinishButton.IsEnabled = true;
        }
    }

    private void Ticker_Tick(object? sender, EventArgs e)
    {
        if (_active is null) return;
        var seconds = _active.AccumulatedTodayMinutes * 60 + (DateTime.Now - _activeFetchedAt).TotalSeconds;
        ElapsedLabel.Text = FormatHelper.Hms(seconds);
    }

    // ---------- Handlers ----------
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAllAsync();

    private async void DatePick_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) await LoadDayAsync();
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        var type = (TypeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "SO";
        var number = NumberBox.Text.Trim();
        if (string.IsNullOrEmpty(number))
        {
            MessageBox.Show(this, "Informe o número da solicitação.", "Atenção");
            return;
        }

        int? clientId = null;
        string? clientName = null;
        if (ClientCombo.SelectedItem is ClientDto c) clientId = c.Id;
        else if (!string.IsNullOrWhiteSpace(ClientCombo.Text)) clientName = ClientCombo.Text.Trim();

        var title = string.IsNullOrWhiteSpace(TitleBox.Text) ? null : TitleBox.Text.Trim();

        try
        {
            await _api.CreateSolicitationAsync(type, number, clientId, clientName, title);
            NumberBox.Clear();
            TitleBox.Clear();
            ClientCombo.SelectedItem = null;
            ClientCombo.Text = "";
            await RefreshAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (SolGrid.SelectedItem is not SolicitationDto sol)
        {
            MessageBox.Show(this, "Selecione uma solicitação na lista.", "Atenção");
            return;
        }
        try
        {
            _active = await _api.StartAsync(sol.Id);
            _activeFetchedAt = DateTime.Now;
            _lastSolicitationId = sol.Id;
            UpdateTimerPanel();
            _widget?.Sync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Pause_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _api.PauseAsync();
            await RefreshActiveAsync();
            await LoadDayAsync();
            _widget?.Sync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro");
        }
    }

    private async void Finish_Click(object sender, RoutedEventArgs e)
    {
        var id = _active?.SolicitationId ?? _lastSolicitationId;
        if (id <= 0) return;
        try
        {
            await _api.FinishAsync(id);
            await RefreshActiveAsync();
            await LoadDayAsync();
            _widget?.Sync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro");
        }
    }

    private async void AddEvidence_Click(object sender, RoutedEventArgs e)
    {
        if (SolGrid.SelectedItem is not SolicitationDto sol)
        {
            MessageBox.Show(this, "Selecione uma solicitação na lista.", "Atenção");
            return;
        }
        var value = EvidenceBox.Text.Trim();
        if (string.IsNullOrEmpty(value)) return;
        try
        {
            await _api.AddLinkEvidenceAsync(sol.Id, value, null);
            EvidenceBox.Clear();
            MessageBox.Show(this, "Evidência adicionada.", "Delta Apont");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro");
        }
    }

    private void OpenWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_widget is null || !_widget.IsVisible)
        {
            _widget = new TimerWidget(_api);
            _widget.Show();
        }
        else
        {
            _widget.Activate();
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        Session.Current = null;
        new LoginWindow().Show();
        Close();
    }
}
