const DOWNLOAD_URL =
  "https://github.com/alephdias/delta_apont_app/releases/latest/download/DeltaDecisao-Desktop.exe";

const steps = [
  {
    t: "Baixe e abra",
    d: "É um único arquivo — não precisa instalar. Guarde onde preferir e abra com um duplo clique.",
  },
  {
    t: "Entre com a conta da empresa",
    d: "Use o mesmo e-mail e senha da web. Os apontamentos são os mesmos nos dois lados.",
  },
  {
    t: "Cadastre a SO ou PA e inicie",
    d: "Informe o número, escolha o cliente e clique em iniciar. O cronômetro começa a contar.",
  },
  {
    t: "Deixe a janelinha no canto",
    d: "Uma janela pequena fica sempre à vista. Pause e continue conforme troca de atendimento.",
  },
  {
    t: "Confira o dia na web",
    d: "No fim do dia, ajuste os tempos em múltiplos de 15 aqui na web. O fechamento se monta sozinho.",
  },
];

const features = [
  { t: "Cronômetro de verdade", d: "Iniciar, pausar, continuar e finalizar. Um atendimento por vez." },
  { t: "Janela flutuante", d: "Fica no topo, num canto do monitor, com o tempo sempre correndo." },
  { t: "SO e PA por cliente", d: "Organize as solicitações por empresa e filtre pelo dia." },
  { t: "Tempo em quartos", d: "Real e apontado lado a lado, arredondando para múltiplos de 15." },
  { t: "Notas e evidências", d: "Registre observações e links direto na solicitação." },
  { t: "Sincroniza com a web", d: "O que você cronometra aparece no seu “Meu dia”." },
];

export function AplicativoPage() {
  return (
    <div>
      <div className="page-head">
        <div>
          <span className="eyebrow">Aplicativo desktop</span>
          <h2>O caderno + cronômetro</h2>
        </div>
      </div>

      <section className="app-hero">
        <div>
          <p className="app-hero-lead">
            Deixe o cronômetro rodando num canto do monitor enquanto você atende. Registre suas
            SOs e PAs, ajuste o tempo e, no fim do dia, o fechamento já aparece aqui na web.
          </p>
          <a className="btn btn-ink btn-lg" href={DOWNLOAD_URL}>
            ↓ Baixar para Windows
          </a>
          <div className="download-meta">Windows 10/11 · arquivo único · versão mais recente</div>
        </div>

        {/* Maquete da janela flutuante — a assinatura do desktop */}
        <div className="widget-mock" aria-hidden="true">
          <div className="wm-top">
            <span className="wm-dot" />
            <span className="wm-code">PA-26024321</span>
            <span className="wm-x">✕</span>
          </div>
          <div className="wm-time">01:15:20</div>
          <div className="wm-actions">
            <span className="wm-btn">⏸</span>
            <span className="wm-btn">▶</span>
            <span className="wm-btn wm-btn-wide">■ Fim</span>
          </div>
        </div>
      </section>

      <div className="section-label">
        <span className="eyebrow">Como funciona</span>
        <span className="eyebrow">5 passos</span>
      </div>
      <div className="steps">
        {steps.map((s, i) => (
          <div className="step" key={s.t}>
            <span className="step-num">{String(i + 1).padStart(2, "0")}</span>
            <div>
              <h4>{s.t}</h4>
              <p>{s.d}</p>
            </div>
          </div>
        ))}
      </div>

      <div className="section-label" style={{ marginTop: "1.75rem" }}>
        <span className="eyebrow">O que ele faz</span>
      </div>
      <div className="feat-grid">
        {features.map((f) => (
          <div className="feat" key={f.t}>
            <h4>{f.t}</h4>
            <p>{f.d}</p>
          </div>
        ))}
      </div>

      <p className="app-foot">
        Use a mesma conta da empresa. Por enquanto, apenas Windows.
      </p>
    </div>
  );
}
