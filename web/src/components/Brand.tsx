/** Nome e marca centralizados. A logo é branca (para fundo escuro);
 *  no tema claro ela é escurecida via filtro CSS (.brand-logo). */
export const APP_NAME = "Delta Decisão";

export function Logo({ size = "sm" }: { size?: "sm" | "lg" }) {
  return (
    <img
      src="/logo.png"
      alt={APP_NAME}
      className={size === "lg" ? "brand-logo brand-logo-lg" : "brand-logo"}
    />
  );
}
