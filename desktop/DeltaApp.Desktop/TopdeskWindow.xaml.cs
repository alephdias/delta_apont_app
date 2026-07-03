using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace DeltaApp.Desktop;

/// <summary>
/// Bancada de descoberta do TOPdesk: navegador embutido (WebView2) com sessão
/// persistente (login uma vez, sem guardar senha) + botões pra capturar o HTML/campos
/// das telas e um console de JS. Serve pra mapear os seletores reais antes de
/// escrever a automação do apontamento.
/// </summary>
public partial class TopdeskWindow : Window
{
    private const string LoginUrl = "https://suporte.deltadecisao.com.br/tas/public/login/form";

    private static string CapturesDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeltaApont", "topdesk-captures");

    private static string UserDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeltaApont", "webview2");

    public TopdeskWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await InitAsync();
        Closed += (_, _) => _batchTcs?.TrySetResult(BatchAction.Stop);
    }

    private bool Ready => Web.CoreWebView2 is not null;

    private async Task InitAsync()
    {
        try
        {
            Status.Text = "iniciando navegador...";
            Directory.CreateDirectory(UserDataDir);
            var env = await CoreWebView2Environment.CreateAsync(null, UserDataDir);
            await Web.EnsureCoreWebView2Async(env);
            Web.CoreWebView2.Navigate(LoginUrl);
            Status.Text = "pronto — entre como operador (a sessão fica salva)";
        }
        catch (Exception ex)
        {
            Status.Text = "erro ao iniciar WebView2";
            MessageBox.Show(this,
                "Não consegui iniciar o navegador embutido (WebView2).\n\n" + ex.Message +
                "\n\nSe faltar o runtime, instale o 'Evergreen WebView2 Runtime' da Microsoft.",
                "TOPdesk", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        if (Ready) Web.CoreWebView2.Navigate(LoginUrl);
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!Ready) return;
        var v = SoBox.Text.Trim();
        if (string.IsNullOrEmpty(v)) return;

        if (v.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            Web.CoreWebView2.Navigate(v);
            return;
        }

        await OpenSolicitationAsync(v);
    }

    /// <summary>Abre uma solicitação pelo código (ex.: SO-25081225) usando a busca do TOPdesk.</summary>
    private async Task OpenSolicitationAsync(string code)
    {
        try
        {
            Status.Text = $"abrindo {code}...";
            // abre o painel de busca (o ícone da lupa)
            if (!await CdpClickHandleAsync("tas.reference.search"))
                await CdpClickHandleAsync("search_menu");
            await Task.Delay(700);

            // digita o número e busca (o dropdown já vem em "Solicitações")
            var f = await FocusSelectAsync("search_input");
            if (f.Contains("nf"))
            {
                Status.Text = "não achei o campo de busca (search_input)";
                return;
            }
            await CdpTypeCharsAsync(code);
            await Task.Delay(300);
            await CdpKeyAsync("Enter", "Enter", 13);
            Status.Text = $"buscando {code}... (aguarde a solicitação abrir)";
        }
        catch (Exception ex) { Status.Text = "erro ao abrir: " + ex.Message; }
    }

    // ----- Clique real via CDP (os botões do TOPdesk são widgets; .click() do JS não basta) -----

    /// <summary>Centro do elemento (pelo handle) em coords do viewport, somando offsets dos iframes.</summary>
    private async Task<(double x, double y)?> GetCenterAsync(string handle)
    {
        // Ignora elementos invisíveis (rect zerado): com vários cards abertos no mango,
        // os cards de fundo têm os MESMOS handles — só o card ativo tem tamanho.
        var js =
            "(function(){function find(h,d){d=d||document;var es=d.querySelectorAll('[handle=\"'+h+'\"]');" +
            "for(var k=0;k<es.length;k++){var r=es[k].getBoundingClientRect();if(r.width>0&&r.height>0)return es[k];}" +
            "var f=d.querySelectorAll('iframe,frame');for(var i=0;i<f.length;i++){try{var c=f[i].contentDocument;if(c){var r=find(h,c);if(r)return r;}}catch(e){}}return null;}" +
            "var el=find('" + handle + "');if(!el)return 'null';" +
            "function abs(el){var r=el.getBoundingClientRect();var x=r.left,y=r.top;var w=el.ownerDocument.defaultView;" +
            "while(w&&w.frameElement){var fr=w.frameElement.getBoundingClientRect();x+=fr.left;y+=fr.top;w=(w.parent!==w)?w.parent:null;}" +
            "return{x:x+r.width/2,y:y+r.height/2};}var c=abs(el);return JSON.stringify(c);})()";
        var res = await Web.CoreWebView2.ExecuteScriptAsync(js);
        try
        {
            using var doc = JsonDocument.Parse(res);
            var s = doc.RootElement.ValueKind == JsonValueKind.String ? doc.RootElement.GetString() : res;
            if (string.IsNullOrEmpty(s) || s == "null") return null;
            using var d2 = JsonDocument.Parse(s);
            return (d2.RootElement.GetProperty("x").GetDouble(), d2.RootElement.GetProperty("y").GetDouble());
        }
        catch { return null; }
    }

    private async Task CdpMouseClickAsync(double x, double y)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        string ev(string type) =>
            $"{{\"type\":\"{type}\",\"x\":{x.ToString(ci)},\"y\":{y.ToString(ci)},\"button\":\"left\",\"buttons\":1,\"clickCount\":1}}";
        await Web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent", ev("mousePressed"));
        await Web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchMouseEvent", ev("mouseReleased"));
    }

    private async Task<bool> CdpClickHandleAsync(string handle)
    {
        var c = await GetCenterAsync(handle);
        if (c is null) return false;
        await CdpMouseClickAsync(c.Value.x, c.Value.y);
        return true;
    }

    private async void Capture_Click(object sender, RoutedEventArgs e)
    {
        if (!Ready) return;
        try
        {
            var htmlJson = await Web.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
            var html = JsonSerializer.Deserialize<string>(htmlJson) ?? "";
            var frames = await Web.CoreWebView2.ExecuteScriptAsync(
                "JSON.stringify(Array.from(document.querySelectorAll('iframe,frame')).map(f=>({src:f.src,id:f.id,name:f.name})))");

            Directory.CreateDirectory(CapturesDir);
            var file = Path.Combine(CapturesDir, $"pagina-{DateTime.Now:yyyyMMdd-HHmmss}.html");
            File.WriteAllText(file,
                $"<!-- URL: {Web.CoreWebView2.Source} -->\n<!-- FRAMES: {frames} -->\n" + html);

            Status.Text = $"salvo: {Path.GetFileName(file)}";
            OpenInExplorer(file);
        }
        catch (Exception ex) { Status.Text = "erro na captura: " + ex.Message; }
    }

    private async void CaptureFields_Click(object sender, RoutedEventArgs e)
    {
        if (!Ready) return;
        // Desce recursivamente nos iframes do mesmo domínio (o mango do TOPdesk aninha cards/modais em iframes).
        // Mira atributos semânticos estáveis (handle, aria-label, title, mtype) em vez dos ids embaralhados.
        const string js =
            "(function(){function walk(d,p,o){try{var e=d.querySelectorAll('input,select,textarea,button,a,[role=button],[handle],[aria-label]');" +
            "for(var i=0;i<e.length;i++){var el=e[i];var r=el.getBoundingClientRect();o.push({f:p,tag:el.tagName,type:el.getAttribute('type')," +
            "handle:el.getAttribute('handle'),aria:el.getAttribute('aria-label'),title:el.getAttribute('title'),mtype:el.getAttribute('mtype')," +
            "name:el.getAttribute('name'),val:(el.value||'').toString().slice(0,40),txt:(el.innerText||'').toString().trim().slice(0,60)," +
            "w:Math.round(r.width),h:Math.round(r.height)});}var fr=d.querySelectorAll('iframe,frame');for(var j=0;j<fr.length;j++){" +
            "try{var cd=fr[j].contentDocument;if(cd)walk(cd,p+'/'+(fr[j].getAttribute('handle')||fr[j].id||j),o);}catch(x){o.push({f:p,err:'noaccess'});}}}catch(y){}}" +
            "var out=[];walk(document,'top',out);return JSON.stringify(out.slice(0,900));})()";
        try
        {
            var json = await Web.CoreWebView2.ExecuteScriptAsync(js);
            var pretty = Pretty(json);
            Directory.CreateDirectory(CapturesDir);
            var file = Path.Combine(CapturesDir, $"campos-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(file, pretty);
            JsResult.Text = pretty;
            Status.Text = $"salvo: {Path.GetFileName(file)}";
            OpenInExplorer(file);
        }
        catch (Exception ex) { Status.Text = "erro: " + ex.Message; }
    }

    // ----- Preenchimento com digitação real (CDP) -----
    // Os widgets SmartClient do TOPdesk ignoram value setado por JS; só validam com
    // teclas de verdade. Usamos o DevTools Protocol do WebView2 pra "digitar".

    private async Task CdpInsertTextAsync(string text)
    {
        var p = JsonSerializer.Serialize(new { text });
        await Web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.insertText", p);
    }

    /// <summary>Digita tecla a tecla (dispara os handlers de filtro dos comboboxes SmartClient).</summary>
    private async Task CdpTypeCharsAsync(string text)
    {
        foreach (var ch in text)
        {
            var s = ch.ToString();
            var down = JsonSerializer.Serialize(new { type = "keyDown", key = s, text = s });
            var up = JsonSerializer.Serialize(new { type = "keyUp", key = s });
            await Web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", down);
            await Web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", up);
            await Task.Delay(35);
        }
    }

    private async Task CdpKeyAsync(string key, string code, int vk)
    {
        string mk(string type) =>
            $"{{\"type\":\"{type}\",\"key\":\"{key}\",\"code\":\"{code}\",\"windowsVirtualKeyCode\":{vk},\"nativeVirtualKeyCode\":{vk}}}";
        await Web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", mk("keyDown"));
        await Web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", mk("keyUp"));
    }

    /// <summary>Acha o campo pelo handle (descendo nos iframes), foca e seleciona o conteúdo.</summary>
    private async Task<string> FocusSelectAsync(string handle)
    {
        var js =
            "(function(){function find(h,d){d=d||document;var es=d.querySelectorAll('[handle=\"'+h+'\"]');" +
            "for(var k=0;k<es.length;k++){var r=es[k].getBoundingClientRect();if(r.width>0&&r.height>0)return es[k];}" +
            "var f=d.querySelectorAll('iframe,frame');for(var i=0;i<f.length;i++){try{var c=f[i].contentDocument;if(c){var r=find(h,c);if(r)return r;}}catch(e){}}return null;}" +
            "var el=find('" + handle + "');if(!el)return 'nf';" +
            "if(el.tagName!=='INPUT'&&el.tagName!=='TEXTAREA'){var inp=el.querySelector('input,textarea');if(inp)el=inp;}" +
            "el.focus();try{el.setSelectionRange(0,(el.value||'').length);}catch(e){}return 'ok';})()";
        return await Web.CoreWebView2.ExecuteScriptAsync(js);
    }

    /// <summary>Lê de volta os valores gravados nos campos (verdade de fato, não pelo pixel).</summary>
    private async Task<string> ReadValuesAsync()
    {
        var js =
            "(function(){function find(h,d){d=d||document;var es=d.querySelectorAll('[handle=\"'+h+'\"]');" +
            "for(var k=0;k<es.length;k++){var r=es[k].getBoundingClientRect();if(r.width>0&&r.height>0)return es[k];}" +
            "var f=d.querySelectorAll('iframe,frame');for(var i=0;i<f.length;i++){try{var c=f[i].contentDocument;if(c){var r=find(h,c);if(r)return r;}}catch(e){}}return null;}" +
            "var hs=['trtimetaken_container_container','trreason','trnotes'];var o={};" +
            "hs.forEach(function(h){var el=find(h);if(el&&el.tagName!=='INPUT'&&el.tagName!=='TEXTAREA'){var i=el.querySelector('input,textarea');if(i)el=i;}o[h]=el?el.value:'nf';});" +
            "return JSON.stringify(o);})()";
        return await Web.CoreWebView2.ExecuteScriptAsync(js);
    }

    /// <summary>Preenche o modal "Registrar tempo gasto" (deixa o Registrar pro usuário).</summary>
    private async Task FillTimeModalAsync(string tempo, string motivo, string notes)
    {
        await FocusSelectAsync("trtimetaken_container_container");
        await CdpTypeCharsAsync(tempo);
        await CdpKeyAsync("Tab", "Tab", 9);
        await Task.Delay(400);

        await FocusSelectAsync("trnotes");
        await CdpTypeCharsAsync(notes);
        await CdpKeyAsync("Tab", "Tab", 9);
        await Task.Delay(400);

        // Motivo (combobox): digita e clica na opção com texto EXATAMENTE igual ao motivo
        // (evita pegar variantes tipo "Atendimento #D" quando existe "Atendimento" puro).
        await FocusSelectAsync("trreason");
        await CdpTypeCharsAsync(motivo);
        await Task.Delay(900);

        var opt = await GetCenterByExactTextAsync(motivo);
        if (opt is not null)
        {
            await CdpMouseClickAsync(opt.Value.x, opt.Value.y);
        }
        else
        {
            // fallback: destaca o 1º da lista e confirma
            await CdpKeyAsync("ArrowDown", "ArrowDown", 40);
            await Task.Delay(200);
            await CdpKeyAsync("Enter", "Enter", 13);
        }
        await Task.Delay(400);
    }

    /// <summary>Acha um elemento-folha cujo texto é EXATAMENTE igual (ex.: uma opção de dropdown).</summary>
    private async Task<(double x, double y)?> GetCenterByExactTextAsync(string text)
    {
        var esc = text.Replace("\\", "\\\\").Replace("'", "\\'");
        var js =
            "(function(){function abs(el){var r=el.getBoundingClientRect();var x=r.left,y=r.top;var w=el.ownerDocument.defaultView;" +
            "while(w&&w.frameElement){var fr=w.frameElement.getBoundingClientRect();x+=fr.left;y+=fr.top;w=(w.parent!==w)?w.parent:null;}return{x:x+r.width/2,y:y+r.height/2};}" +
            "function walk(d){var all=d.querySelectorAll('*');for(var i=0;i<all.length;i++){var e=all[i];if(e.childElementCount>0)continue;" +
            "var t=(e.innerText||e.textContent||'').trim();if(t==='" + esc + "'){var r=e.getBoundingClientRect();if(r.width>0&&r.height>0)return e;}}" +
            "var fr=d.querySelectorAll('iframe,frame');for(var j=0;j<fr.length;j++){try{var c=fr[j].contentDocument;if(c){var r=walk(c);if(r)return r;}}catch(e){}}return null;}" +
            "var el=walk(document);if(!el)return 'null';return JSON.stringify(abs(el));})()";
        var res = await Web.CoreWebView2.ExecuteScriptAsync(js);
        try
        {
            using var doc = JsonDocument.Parse(res);
            var s = doc.RootElement.ValueKind == JsonValueKind.String ? doc.RootElement.GetString() : res;
            if (string.IsNullOrEmpty(s) || s == "null") return null;
            using var d2 = JsonDocument.Parse(s);
            return (d2.RootElement.GetProperty("x").GetDouble(), d2.RootElement.GetProperty("y").GetDouble());
        }
        catch { return null; }
    }

    /// <summary>Abre a aba REGISTRO DE TEMPO e clica em "Registro de tempo" pra abrir o modal.</summary>
    private async Task<bool> OpenTimeModalAsync()
    {
        // Espera a aba aparecer (o card carrega num iframe novo, pode demorar)
        var tab = await WaitCenterByTextAsync("tabbutton", "REGISTRO DE TEMPO", 15, 400);
        if (tab is null) { Status.Text = "não achei a aba REGISTRO DE TEMPO"; return false; }
        await CdpMouseClickAsync(tab.Value.x, tab.Value.y);

        // Espera o botão da aba renderizar
        var btn = await WaitCenterAsync("time_registration.explorer.tooltip.new_record", 15, 400);
        if (btn is null) { Status.Text = "não achei o botão 'Registro de tempo' (após abrir a aba)"; return false; }
        await CdpMouseClickAsync(btn.Value.x, btn.Value.y);
        await Task.Delay(1200);
        return true;
    }

    private async Task<(double x, double y)?> WaitCenterAsync(string handle, int tries = 12, int delayMs = 400)
    {
        for (int i = 0; i < tries; i++)
        {
            var c = await GetCenterAsync(handle);
            if (c is not null) return c;
            await Task.Delay(delayMs);
        }
        return null;
    }

    private async Task<(double x, double y)?> WaitCenterByTextAsync(string mtype, string text, int tries = 12, int delayMs = 400)
    {
        for (int i = 0; i < tries; i++)
        {
            var c = await GetCenterByTextAsync(mtype, text);
            if (c is not null) return c;
            await Task.Delay(delayMs);
        }
        return null;
    }

    /// <summary>Acha um elemento por mtype + prefixo de texto (a aba não tem handle).</summary>
    private async Task<(double x, double y)?> GetCenterByTextAsync(string mtype, string textPrefix)
    {
        var js =
            "(function(){function abs(el){var r=el.getBoundingClientRect();var x=r.left,y=r.top;var w=el.ownerDocument.defaultView;" +
            "while(w&&w.frameElement){var fr=w.frameElement.getBoundingClientRect();x+=fr.left;y+=fr.top;w=(w.parent!==w)?w.parent:null;}return{x:x+r.width/2,y:y+r.height/2};}" +
            "function walk(d){var els=d.querySelectorAll('[mtype=\"" + mtype + "\"]');for(var i=0;i<els.length;i++){var e=els[i];var t=(e.innerText||'').trim();" +
            "if(t.indexOf('" + textPrefix + "')===0&&e.getBoundingClientRect().width>0)return e;}" +
            "var fr=d.querySelectorAll('iframe,frame');for(var j=0;j<fr.length;j++){try{var c=fr[j].contentDocument;if(c){var r=walk(c);if(r)return r;}}catch(e){}}return null;}" +
            "var el=walk(document);if(!el)return 'null';return JSON.stringify(abs(el));})()";
        var res = await Web.CoreWebView2.ExecuteScriptAsync(js);
        try
        {
            using var doc = JsonDocument.Parse(res);
            var s = doc.RootElement.ValueKind == JsonValueKind.String ? doc.RootElement.GetString() : res;
            if (string.IsNullOrEmpty(s) || s == "null") return null;
            using var d2 = JsonDocument.Parse(s);
            return (d2.RootElement.GetProperty("x").GetDouble(), d2.RootElement.GetProperty("y").GetDouble());
        }
        catch { return null; }
    }

    /// <summary>Navegador pronto e logado como operador? (mostra aviso se não)</summary>
    private async Task<bool> EnsureLoggedInAsync()
    {
        // espera o navegador iniciar (janela recém-aberta)
        for (int i = 0; i < 25 && !Ready; i++) await Task.Delay(300);
        if (!Ready) { Status.Text = "o navegador ainda não iniciou — tente de novo"; return false; }

        var url = Web.CoreWebView2.Source ?? "";
        if (url.Contains("/login", StringComparison.OrdinalIgnoreCase) || url.Contains("/public/", StringComparison.OrdinalIgnoreCase))
        {
            Status.Text = "faça login como operador e clique de novo";
            MessageBox.Show(this,
                "Entre como operador no TOPdesk (aqui nesta janela) e depois clique em 'Lançar no TOPdesk' novamente.",
                "TOPdesk");
            return false;
        }
        return true;
    }

    /// <summary>Fluxo real: abre a SO, abre o modal e preenche com o tempo/observação da solicitação.
    /// Devolve true se o modal foi preenchido (falta só o usuário conferir e Registrar).</summary>
    public async Task<bool> LancarApontamentoAsync(string code, string tempoHHMM, string observacao)
    {
        Activate();
        if (!await EnsureLoggedInAsync()) return false;

        try
        {
            Status.Text = $"lançando {code} ({tempoHHMM})...";
            await OpenSolicitationAsync(code);
            await Task.Delay(3000);
            if (!await OpenTimeModalAsync()) return false;
            await FillTimeModalAsync(tempoHHMM, "Atendimento", observacao);
            Status.Text = $"{code}: preenchido ({tempoHHMM}). Confira e clique em Registrar no TOPdesk.";
            return true;
        }
        catch (Exception ex) { Status.Text = "erro ao lançar: " + ex.Message; return false; }
    }

    // ----- Lote: lança várias solicitações, parando em cada uma pro usuário Registrar -----

    public record LoteItem(string Code, string TempoHHMM, string Observacao);

    private enum BatchAction { Next, Skip, Stop }

    private TaskCompletionSource<BatchAction>? _batchTcs;

    /// <summary>Lança os itens um a um; entre um e outro, espera o usuário conferir/Registrar
    /// e clicar "Próxima" na barra de lote (humano no loop, como no lançamento individual).</summary>
    public async Task LancarLoteAsync(IReadOnlyList<LoteItem> itens)
    {
        Activate();
        if (_batchTcs is not null) { Status.Text = "já existe um lote em andamento"; return; }
        if (!await EnsureLoggedInAsync()) return;

        int feitas = 0;
        var puladas = new List<string>();

        try
        {
            for (int i = 0; i < itens.Count; i++)
            {
                var item = itens[i];
                BatchText.Text = $"{i + 1}/{itens.Count} · lançando {item.Code} ({item.TempoHHMM})...";
                BatchBar.Visibility = Visibility.Visible;
                BatchNext.Visibility = Visibility.Collapsed;
                BatchSkip.Visibility = Visibility.Collapsed;

                var ok = await LancarApontamentoAsync(item.Code, item.TempoHHMM, item.Observacao);

                if (ok)
                {
                    BatchText.Text = $"{i + 1}/{itens.Count} · {item.Code} preenchida ({item.TempoHHMM}). " +
                                     "Confira e clique em Registrar no TOPdesk; depois, aqui em \"Registrei, próxima\".";
                    BatchNext.Visibility = Visibility.Visible;
                }
                else
                {
                    BatchText.Text = $"{i + 1}/{itens.Count} · {item.Code} falhou: {Status.Text}";
                    BatchSkip.Visibility = Visibility.Visible;
                }

                _batchTcs = new TaskCompletionSource<BatchAction>(TaskCreationOptions.RunContinuationsAsynchronously);
                var action = await _batchTcs.Task;
                _batchTcs = null;

                if (action == BatchAction.Stop) { puladas.AddRange(itens.Skip(i + 1).Select(x => x.Code)); break; }
                if (ok && action != BatchAction.Skip) feitas++;
                else puladas.Add(item.Code);
            }
        }
        finally
        {
            _batchTcs = null;
            BatchBar.Visibility = Visibility.Collapsed;
            Status.Text = $"lote: {feitas} preenchida(s)" +
                          (puladas.Count > 0 ? $", {puladas.Count} pulada(s): {string.Join(", ", puladas)}" : ".");
        }
    }

    private void BatchNext_Click(object sender, RoutedEventArgs e) => _batchTcs?.TrySetResult(BatchAction.Next);
    private void BatchSkip_Click(object sender, RoutedEventArgs e) => _batchTcs?.TrySetResult(BatchAction.Skip);
    private void BatchStop_Click(object sender, RoutedEventArgs e) => _batchTcs?.TrySetResult(BatchAction.Stop);

    private async void RunJs_Click(object sender, RoutedEventArgs e)
    {
        if (!Ready) return;
        if (string.IsNullOrWhiteSpace(JsBox.Text)) return;
        try
        {
            var r = await Web.CoreWebView2.ExecuteScriptAsync(JsBox.Text);
            JsResult.Text = Pretty(r);
        }
        catch (Exception ex) { JsResult.Text = "erro: " + ex.Message; }
    }

    private static void OpenInExplorer(string file)
    {
        try { System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file}\""); }
        catch { /* melhor esforço */ }
    }

    /// <summary>ExecuteScriptAsync devolve JSON; se for uma string contendo JSON, embeleza.</summary>
    private static string Pretty(string json)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                var inner = doc.RootElement.GetString() ?? "";
                try { using var d2 = JsonDocument.Parse(inner); return JsonSerializer.Serialize(d2.RootElement, opts); }
                catch { return inner; }
            }
            return JsonSerializer.Serialize(doc.RootElement, opts);
        }
        catch { return json; }
    }
}
