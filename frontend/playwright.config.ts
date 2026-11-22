import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  reporter: "list",
  use: {
    ...devices["Desktop Edge"],
    channel: "msedge",
    baseURL: process.env.E2E_BASE_URL ?? "http://localhost:3100",
    headless: true,
  },
});
