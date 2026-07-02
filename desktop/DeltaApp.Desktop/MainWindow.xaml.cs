using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using DeltaApp.Desktop.Models;
using DeltaApp.Desktop.Services;

namespace DeltaApp.Desktop;

public partial class MainWindow : Window
{
    private readonly ApiClient _api = new();
    private readonly TimerHub _timer;
    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromSeconds(1) };

    private List<ClientDto> _clients = new();
    private List<SolicitationDto> _solicitations = new();
    private List<DayEntryDto> _dayEntries = new();
    private TimerWidget? _widget;
    private int _targetMinutes = 360;

    private const int HotkeyId = 9000;
    private const uint VkP = 0x50; // tecla P
    private HwndSource? _source;
    private int _tickCount;
    private bool _autoHandling;
    private static readonly TimeSpan IdleThreshold = TimeSpan.FromMinutes(10);

    public MainWindow()
    {
        InitializeComponent();
        _timer = new TimerHub(_api);
        _timer.Changed += OnTimerChanged;

        EmailText.Text = Session.Current?.Email ?? "";
        DatePick.SelectedDate = DateTime.Today;
        AutoPauseCheck.IsChecked = Settings.AutoPauseWhenIdle;

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
    }

    private DateOnly SelectedDate => DateOnly.FromDateTime(DatePick.SelectedDate ?? DateTime.Today);

    private async Task RefreshAllAsync()
    {
        try
        {
            var profile = await _api.GetProfileAsync();
            if (profile is not null) _targetMinutes = profile.DailyTargetMinutes;

            _clients = await _api.GetClientsAsync() ?? new();
            ClientCombo.ItemsSource = _clients;
            await LoadSolicitationsAsync();
            await LoadDayAsync();
            await _timer.RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task LoadSolicitationsAsync()
    {
        DateOnly? date = OnlyDayCheck.IsChecked == true ? SelectedDate : null;
        _solicitations = await _api.GetSolicitationsAsync(date) ?? new();
        SolGrid.ItemsSource = _solicitations;
    }

    private async void OnlyDay_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) await LoadSolicitationsAsync();
    }

    private void SolGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SolGrid.SelectedItem is SolicitationDto sol)
        {
            var client = string.IsNullOrEmpty(sol.ClientName) ? "sem empresa" : sol.ClientName;
            SelectedHeader.Text = $"{sol.Code}  ·  {client}";
            NotesBox.Text = sol.Description ?? "";
        }
        else
        {
            SelectedHeader.Text = "Nenhuma solicitação selecionada";
            NotesBox.Text = "";
        }
    }

    private async void SaveNotes_Click(object sender, RoutedEventArgs e)
    {
        if (SolGrid.SelectedItem is not SolicitationDto sol)
        {
            MessageBox.Show(this, "Selecione uma solicitação na lista.", "Atenção");
            return;
        }
        try
        {
            var text = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
            await _api.UpdateNotesAsync(sol.Id, text);
            sol.Description = text;
            MessageBox.Show(this, "Observações salvas.", "Delta Decisão");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erro"); }
    }

    private async Task LoadDayAsync()
    {
        var entries = await _api.GetDayEntriesAsync(SelectedDate) ?? new();
        _dayEntries = entries;
        DayGrid.ItemsSource = entries;
        var real = entries.Sum(e => e.RealMinutes);
        var adj = entries.Sum(e => e.AdjustedMinutes);
        var falta = Math.Max(0, _targetMinutes - adj);
        DayTotals.Text = $"Real {FormatHelper.Minutes(real)}   ·   Apontado {FormatHelper.Minutes(adj)}   ·   "
                         + (falta > 0 ? $"Falta {FormatHelper.Minutes(falta)} p/ meta ({FormatHelper.Minutes(_targetMinutes)})" : "meta batida ✓");
        RenderMeter(adj, _targetMinutes);
    }

    /// <summary>Medidor de quartos de hora: cada tick = 15 min, verde até o apontado, marca da meta.</summary>
    private void RenderMeter(int adjustedMinutes, int targetMinutes)
    {
        DayMeter.Children.Clear();
        var target = Math.Max(15, targetMinutes);
        var filled = Math.Max(0, adjustedMinutes / 15);
        var targetTicks = target / 15;
        var total = Math.Max(targetTicks, filled);
        var green = (Brush)FindResource("Green");
        var greenDeep = (Brush)FindResource("GreenDeep");
        var line = (Brush)FindResource("Line2");
        var ink = (Brush)FindResource("Ink");

        for (var i = 0; i < total; i++)
        {
            var tick = new Border
            {
                Width = 9,
                Height = 22,
                CornerRadius = new CornerRadius(2),
                Margin = new Thickness(0, 0, 3, 0),
                Background = i < filled ? (i >= targetTicks ? greenDeep : green) : line
            };
            DayMeter.Children.Add(tick);

            // marca da meta (linha de tinta após o último tick da meta)
            if (i == targetTicks - 1)
            {
                DayMeter.Children.Add(new Border
                {
                    Width = 2,
                    Height = 26,
                    Background = ink,
                    Margin = new Thickness(0, 0, 4, 0),
                    CornerRadius = new CornerRadius(1)
                });
            }
        }
    }

    // ---------- Cronômetro (via TimerHub, compartilhado com a janelinha) ----------
    private void OnTimerChanged()
    {
        UpdateTimerPanel();
        UpdateElapsed();
    }

    private void UpdateTimerPanel()
    {
        if (_timer.IsRunning)
        {
            var client = string.IsNullOrEmpty(_timer.Active!.ClientName) ? "" : $"  ·  {_timer.Active.ClientName}";
            RunningCode.Text = $"{_timer.Active.Code}{client}";
            RunningCode.Foreground = (Brush)FindResource("GreenDeep");
            PauseButton.IsEnabled = true;
            FinishButton.IsEnabled = true;
        }
        else if (_timer.HasFrozen)
        {
            RunningCode.Text = $"{_timer.LastCode}  ·  pausado";
            RunningCode.Foreground = (Brush)FindResource("Muted");
            PauseButton.IsEnabled = false;
            FinishButton.IsEnabled = true;
        }
        else
        {
            RunningCode.Text = "Nenhum cronômetro ativo";
            RunningCode.Foreground = (Brush)FindResource("Muted");
            PauseButton.IsEnabled = false;
            FinishButton.IsEnabled = false;
        }
    }

    private void UpdateElapsed() => ElapsedLabel.Text = FormatHelper.Hms(_timer.ElapsedSeconds());

    private async void Ticker_Tick(object? sender, EventArgs e)
    {
        UpdateElapsed();
        _tickCount++;

        // Re-sincroniza leve com o servidor a cada 60s.
        if (_tickCount % 60 == 0 && !_autoHandling)
        {
            try { await _timer.RefreshAsync(); } catch { }
        }

        // Auto-pausa por inatividade (10 min).
        if (_tickCount % 20 == 0 && _timer.IsRunning && !_autoHandling
            && Settings.AutoPauseWhenIdle
            && NativeMethods.IdleTime() >= IdleThreshold)
        {
            _autoHandling = true;
            try
            {
                await _timer.PauseAsync();
                await LoadDayAsync();
                RunningCode.Text = "⏸ pausado por inatividade";
            }
            catch { }
            finally { _autoHandling = false; }
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var handle = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(HwndHook);
        // Atalho global Ctrl+Alt+P: pausa/continua mesmo sem foco.
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
            if (_timer.IsRunning) await _timer.PauseAsync();
            else if (_timer.LastSolicitationId > 0) await _timer.StartAsync(_timer.LastSolicitationId);
            await LoadDayAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erro"); }
    }

    // ---------- Handlers ----------
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAllAsync();

    private async void DatePick_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await LoadDayAsync();
        if (OnlyDayCheck.IsChecked == true) await LoadSolicitationsAsync();
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

    private async void ColarEIniciar_Click(object sender, RoutedEventArgs e)
    {
        var text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
        var m = Regex.Match(text, @"\b(SO|PA)[-\s]?(\d{5,})\b", RegexOptions.IgnoreCase);
        if (!m.Success)
        {
            MessageBox.Show(this, "Não encontrei um número de SO/PA na área de transferência.", "Atenção");
            return;
        }
        var type = m.Groups[1].Value.ToUpperInvariant();
        var number = m.Groups[2].Value;
        try
        {
            var sol = await _api.CreateSolicitationAsync(type, number, null, null, null);
            if (sol is null)
            {
                MessageBox.Show(this, "Não foi possível criar a solicitação.", "Erro");
                return;
            }
            await _timer.StartAsync(sol.Id);
            await RefreshAllAsync();
            SolGrid.SelectedItem = _solicitations.FirstOrDefault(s => s.Id == sol.Id);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erro"); }
    }

    private void AutoPause_Changed(object sender, RoutedEventArgs e)
    {
        Settings.AutoPauseWhenIdle = AutoPauseCheck.IsChecked == true;
        Settings.Save();
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
            await _timer.StartAsync(sol.Id);
            await LoadDayAsync();
            await LoadSolicitationsAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erro"); }
    }

    private async void Pause_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _timer.PauseAsync();
            await LoadDayAsync();
            await LoadSolicitationsAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erro"); }
    }

    private async void Finish_Click(object sender, RoutedEventArgs e)
    {
        var id = _timer.Active?.SolicitationId ?? _timer.LastSolicitationId;
        if (id <= 0) return;
        try
        {
            await _timer.FinishAsync(id);
            await LoadDayAsync();
            await LoadSolicitationsAsync();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erro"); }
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
            MessageBox.Show(this, "Evidência adicionada.", "Delta Decisão");
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erro"); }
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
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erro"); }
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
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Erro"); }
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
            _widget = new TimerWidget(_timer);
            _widget.Show();
        }
        else
        {
            _widget.Activate();
        }
    }

    private TopdeskWindow? _topdesk;
    private void OpenTopdesk_Click(object sender, RoutedEventArgs e) => ShowTopdesk();

    private TopdeskWindow ShowTopdesk()
    {
        if (_topdesk is null || !_topdesk.IsVisible)
        {
            _topdesk = new TopdeskWindow { Owner = this };
            _topdesk.Show();
        }
        else
        {
            _topdesk.Activate();
        }
        return _topdesk;
    }

    private async void LancarTopdesk_Click(object sender, RoutedEventArgs e)
    {
        if (SolGrid.SelectedItem is not SolicitationDto sol)
        {
            MessageBox.Show(this, "Selecione uma solicitação na lista.", "Atenção");
            return;
        }

        var entry = _dayEntries.FirstOrDefault(x => x.SolicitationId == sol.Id);
        var adj = entry?.AdjustedMinutes ?? 0;
        if (adj <= 0)
        {
            MessageBox.Show(this,
                $"A solicitação {sol.Code} não tem tempo apontado em {SelectedDate:dd/MM/yyyy}.\n\n" +
                "Cronometre (ou finalize) essa solicitação no dia antes de lançar no TOPdesk.",
                "Nada pra lançar");
            return;
        }

        var tempo = $"{adj / 60}:{adj % 60:00}";                        // 90 -> "1:30"
        var obs = string.IsNullOrWhiteSpace(NotesBox.Text) ? "" : NotesBox.Text.Trim();

        var win = ShowTopdesk();
        await win.LancarApontamentoAsync(sol.Code, tempo, obs);
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
        var (available, tag, _) = await UpdateService.CheckAsync();
        if (!available) return;
        var r = MessageBox.Show(this,
            $"Uma nova versão ({tag}) está disponível.\n\nAtualizar agora? O app vai baixar, atualizar e reiniciar sozinho.",
            "Atualização disponível", MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (r == MessageBoxResult.Yes)
        {
            new UpdateWindow { Owner = this }.Show();
        }
    }
}
