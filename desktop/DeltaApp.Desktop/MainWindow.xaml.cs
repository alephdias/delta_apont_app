using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
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

    private const int HotkeyId = 9000;
    private const uint VkP = 0x50; // tecla P
    private HwndSource? _source;
    private int _tickCount;
    private bool _autoHandling;
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromMinutes(10);

    public MainWindow()
    {
        InitializeComponent();
        EmailText.Text = Session.Current?.Email ?? "";
        DatePick.SelectedDate = DateTime.Today;
        _ticker.Tick += Ticker_Tick;
        _ticker.Start();
        Loaded += async (_, _) =>
        {
            await RefreshAllAsync();
            await CheckUpdateAsync();
        };
        Closed += (_, _) =>
        {
            _ticker.Stop();
            _widget?.Close();
            if (_source is not null)
            {
                NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
                _source.RemoveHook(HwndHook);
            }
        };

        // Ctrl+V cola um print na solicitação selecionada (exceto ao digitar em campos).
        PreviewKeyDown += async (_, e) =>
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control
                && Keyboard.FocusedElement is not TextBox && Clipboard.ContainsImage())
            {
                e.Handled = true;
                await PastePrintAsync();
            }
        };
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

    private async void Ticker_Tick(object? sender, EventArgs e)
    {
        if (_active is not null)
        {
            var seconds = _active.AccumulatedTodayMinutes * 60 + (DateTime.Now - _activeFetchedAt).TotalSeconds;
            ElapsedLabel.Text = FormatHelper.Hms(seconds);
        }

        // A cada ~20s, se estiver rodando e o usuário estiver inativo, pausa sozinho.
        if (++_tickCount % 20 == 0 && _active is not null && !_autoHandling
            && NativeMethods.IdleTime() >= IdleThreshold)
        {
            _autoHandling = true;
            try
            {
                await _api.PauseAsync();
                await RefreshActiveAsync();
                await LoadDayAsync();
                _widget?.Sync();
                RunningCode.Text = "⏸ pausado por inatividade";
            }
            catch { /* silencioso */ }
            finally { _autoHandling = false; }
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(HwndHook);
        // Atalho global Ctrl+Alt+P: pausa/continua o cronômetro mesmo sem foco.
        NativeMethods.RegisterHotKey(handle, HotkeyId, NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, VkP);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            _ = ToggleTimerAsync();
        }
        return IntPtr.Zero;
    }

    private async Task ToggleTimerAsync()
    {
        try
        {
            if (_active is not null)
            {
                await _api.PauseAsync();
                await RefreshActiveAsync();
                await LoadDayAsync();
            }
            else if (_lastSolicitationId > 0)
            {
                _active = await _api.StartAsync(_lastSolicitationId);
                _activeFetchedAt = DateTime.Now;
                UpdateTimerPanel();
            }
            _widget?.Sync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro");
        }
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

    private void ColarSoPa_Click(object sender, RoutedEventArgs e)
    {
        var text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
        var m = Regex.Match(text, @"\b(SO|PA)[-\s]?(\d{5,})\b", RegexOptions.IgnoreCase);
        if (!m.Success)
        {
            MessageBox.Show(this, "Não encontrei um número de SO/PA na área de transferência.", "Atenção");
            return;
        }
        TypeCombo.SelectedIndex = m.Groups[1].Value.ToUpperInvariant() == "PA" ? 1 : 0;
        NumberBox.Text = m.Groups[2].Value;
        NumberBox.Focus();
    }

    private async void AttachFile_Click(object sender, RoutedEventArgs e)
    {
        if (SolGrid.SelectedItem is not SolicitationDto sol)
        {
            MessageBox.Show(this, "Selecione uma solicitação na lista.", "Atenção");
            return;
        }
        var dlg = new OpenFileDialog
        {
            Title = "Anexar evidência",
            Filter = "Arquivos (imagens, PDF, docs)|*.png;*.jpg;*.jpeg;*.gif;*.pdf;*.txt;*.docx;*.xlsx|Todos|*.*"
        };
        if (dlg.ShowDialog(this) != true) return;
        try
        {
            var bytes = await File.ReadAllBytesAsync(dlg.FileName);
            await _api.UploadEvidenceAsync(sol.Id, bytes, Path.GetFileName(dlg.FileName), ContentTypeOf(dlg.FileName));
            MessageBox.Show(this, "Arquivo anexado.", "Delta Decisão");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro");
        }
    }

    private async void PastePrint_Click(object sender, RoutedEventArgs e) => await PastePrintAsync();

    private async Task PastePrintAsync()
    {
        if (SolGrid.SelectedItem is not SolicitationDto sol)
        {
            MessageBox.Show(this, "Selecione uma solicitação na lista.", "Atenção");
            return;
        }
        if (!Clipboard.ContainsImage())
        {
            MessageBox.Show(this, "Não há imagem na área de transferência.", "Atenção");
            return;
        }
        try
        {
            var img = Clipboard.GetImage();
            if (img is null) return;
            using var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(img));
            encoder.Save(ms);
            var name = $"print-{DateTime.Now:yyyyMMdd-HHmmss}.png";
            await _api.UploadEvidenceAsync(sol.Id, ms.ToArray(), name, "image/png", "colado");
            MessageBox.Show(this, "Print anexado.", "Delta Decisão");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro");
        }
    }

    private static string ContentTypeOf(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".pdf" => "application/pdf",
        ".txt" => "text/plain",
        _ => "application/octet-stream",
    };

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
        SessionStore.Clear();
        new LoginWindow().Show();
        Close();
    }

    private async Task CheckUpdateAsync()
    {
        var (available, tag, url) = await UpdateService.CheckAsync();
        if (!available) return;
        var r = MessageBox.Show(this,
            $"Uma nova versão ({tag}) do aplicativo está disponível.\n\nBaixar agora?",
            "Atualização disponível", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (r == MessageBoxResult.Yes)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* ignora */ }
        }
    }
}
