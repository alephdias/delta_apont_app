/**
 * Medidor de quartos de hora — o elemento-assinatura.
 * Cada tick = 15 min. Preenche até o tempo apontado; uma marca de tinta
 * assinala a meta. Ticks além da meta (hora extra) ficam em verde profundo.
 */
export function QuarterMeter({
  adjustedMinutes,
  targetMinutes,
  size = "lg",
  animate = false,
}: {
  adjustedMinutes: number;
  targetMinutes: number;
  size?: "lg" | "sm";
  animate?: boolean;
}) {
  const target = Math.max(15, targetMinutes);
  const filled = Math.max(0, Math.round(adjustedMinutes / 15));
  const targetTicks = Math.round(target / 15);
  const total = Math.max(targetTicks, filled);

  return (
    <div
      className={
        "meter" + (size === "sm" ? " meter-sm" : "") + (animate ? " meter-animate" : "")
      }
    >
      {Array.from({ length: total }, (_, i) => {
        const cls =
          "tick" +
          (i < filled ? " filled" : "") +
          (i >= targetTicks ? " over" : "") +
          (i === targetTicks - 1 ? " target" : "");
        const style = animate
          ? { animationDelay: `${Math.min(i, 40) * 12}ms` }
          : undefined;
        return <span key={i} className={cls} style={style} />;
      })}
    </div>
  );
}
