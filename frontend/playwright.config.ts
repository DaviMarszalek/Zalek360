import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  globalSetup: "./e2e/preparar.ts",
  fullyParallel: false,
  workers: 1,
  reporter: "list",
  use: {
    ...devices["Desktop Edge"],
    channel: "msedge",
    baseURL: process.env.E2E_BASE_URL ?? "http://localhost:3100",
    headless: true,
  },
});
