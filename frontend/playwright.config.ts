import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  globalSetup: "./e2e/preparar.ts",
  fullyParallel: false,
  workers: 1,
  reporter: "list",
  // O ambiente de revisão roda no OneDrive/Docker local e pode responder devagar sob carga;
  // as asserções web-first esperam até 15 s antes de falhar (padrão: 5 s).
  expect: { timeout: 15_000 },
  use: {
    ...devices["Desktop Edge"],
    channel: "msedge",
    baseURL: process.env.E2E_BASE_URL ?? "http://localhost:3100",
    headless: true,
  },
});
