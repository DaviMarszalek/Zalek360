import type { Config } from "tailwindcss";

// Tokens visuais extraídos dos protótipos do MVP 1.
const config: Config = {
  content: ["./app/**/*.{ts,tsx}", "./components/**/*.{ts,tsx}", "./features/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        lateral: { DEFAULT: "#0A1120", ativo: "#172033", texto: "#94A3B8", borda: "#1E293B" },
        primaria: { DEFAULT: "#1D4ED8", escura: "#1E40AF", clara: "#EFF4FF", borda: "#BFD0FB" },
        fundo: "#F5F7FA",
        borda: { DEFAULT: "#E3E7EE", forte: "#CBD2DC" },
        texto: { DEFAULT: "#0F172A", suave: "#475569", fraco: "#64748B" },
      },
      fontFamily: {
        sans: ['"IBM Plex Sans"', "system-ui", "-apple-system", "Segoe UI", "Roboto", "sans-serif"],
        mono: ['"IBM Plex Mono"', "ui-monospace", "SFMono-Regular", "Menlo", "monospace"],
      },
      boxShadow: {
        cartao: "0 1px 2px rgba(15, 23, 42, 0.04)",
        modal: "0 24px 48px -12px rgba(15, 23, 42, 0.28)",
      },
    },
  },
  plugins: [],
};

export default config;
