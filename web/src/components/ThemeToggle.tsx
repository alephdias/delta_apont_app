import { useTheme } from "../theme/ThemeContext";
import type { Theme } from "../theme/ThemeContext";

const options: { value: Theme; icon: string; title: string }[] = [
  { value: "light", icon: "☀", title: "Claro" },
  { value: "dark", icon: "☾", title: "Escuro" },
  { value: "system", icon: "◐", title: "Sistema" },
];

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();
  return (
    <div className="theme-toggle" role="group" aria-label="Tema">
      {options.map((o) => (
        <button
          key={o.value}
          type="button"
          className={theme === o.value ? "on" : ""}
          title={o.title}
          aria-pressed={theme === o.value}
          onClick={() => setTheme(o.value)}
        >
          {o.icon}
        </button>
      ))}
    </div>
  );
}
