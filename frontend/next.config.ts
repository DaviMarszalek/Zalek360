import type { NextConfig } from "next";

// O navegador sempre chama /api/* no próprio frontend; o Next.js encaminha para o backend.
// Assim o cookie httpOnly de sessão fica no mesmo domínio e não há CORS no navegador.
// Em Docker: API_INTERNAL_URL=http://backend:8080 (definido no build). Em desenvolvimento: http://localhost:5000.
const destinoApi = process.env.API_INTERNAL_URL ?? "http://localhost:5000";

const nextConfig: NextConfig = {
  output: "standalone",
  reactStrictMode: true,
  poweredByHeader: false,
  // Uploads de arte (até 20 MB) atravessam o rewrite /api. O Next só bufferiza o corpo quando o proxy.ts roda
  // (o matcher exclui /api), mas o limite explícito evita truncamento silencioso se o matcher mudar.
  experimental: { proxyClientMaxBodySize: "25mb" },
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${destinoApi}/api/:path*` }];
  },
  async headers() {
    return [
      {
        source: "/:path*",
        headers: [
          { key: "X-Content-Type-Options", value: "nosniff" },
          { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
          { key: "X-Frame-Options", value: "DENY" },
        ],
      },
    ];
  },
};

export default nextConfig;
