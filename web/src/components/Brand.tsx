/** Nome e marca centralizados — troque aqui quando a logo definitiva chegar. */
export const APP_NAME = "Delta Decisão";

export function Logo() {
  // Quando houver arquivo de logo em /public, troque por:
  // return <img src="/logo.svg" alt="Delta Decisão" className="brand-logo" />;
  return (
    <span className="delta-mark" aria-hidden="true">
      Δ
    </span>
  );
}
