# Zalek360 Web — Next.js 16 (output standalone)
# Contexto de build: pasta frontend/  →  docker build --build-arg API_INTERNAL_URL=http://backend:8080 ./frontend
# Imagem Debian (glibc): os binários nativos do SWC no package-lock são as variantes linux-*-gnu.

FROM node:22-bookworm-slim AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

FROM node:22-bookworm-slim AS build
WORKDIR /app
# O destino do proxy /api/* é gravado no build (routes-manifest.json); por isso é ARG e não variável de runtime.
ARG API_INTERNAL_URL=http://backend:8080
ENV API_INTERNAL_URL=${API_INTERNAL_URL} \
    NEXT_TELEMETRY_DISABLED=1
COPY --from=deps /app/node_modules ./node_modules
COPY . .
RUN npm run build

FROM node:22-bookworm-slim AS runtime
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0
COPY --from=build --chown=node:node /app/.next/standalone ./
COPY --from=build --chown=node:node /app/.next/static ./.next/static
COPY --from=build --chown=node:node /app/public ./public
USER node
EXPOSE 3000
CMD ["node", "server.js"]
