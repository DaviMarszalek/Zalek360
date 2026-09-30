import type { Metadata, Viewport } from "next";
import "@fontsource/ibm-plex-sans/400.css";
import "@fontsource/ibm-plex-sans/500.css";
import "@fontsource/ibm-plex-sans/600.css";
import "@fontsource/ibm-plex-sans/700.css";
import "@fontsource/ibm-plex-mono/400.css";
import "@fontsource/ibm-plex-mono/500.css";
import "./globals.css";
import { Providers } from "@/components/layout/Providers";

export const metadata: Metadata = {
  title: { default: "Zalek360", template: "%s · Zalek360" },
  description: "Sistema comercial interno da Zalek Personalizados",
  manifest: "/manifest.webmanifest",
  icons: { icon: "/icons/icone.svg", apple: "/icons/icone.svg" },
  robots: { index: false, follow: false },
};

export const viewport: Viewport = { themeColor: "#0A1120", width: "device-width", initialScale: 1 };

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="pt-BR">
      <body><Providers>{children}</Providers></body>
    </html>
  );
}
