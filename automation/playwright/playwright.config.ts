import { defineConfig, devices } from "@playwright/test";
import path from "path";

/**
 * Diretório de saída dos vídeos gravados.
 * Sobrescreva via: VIDEO_DIR=caminho npx playwright test
 */
const VIDEO_DIR = process.env.VIDEO_DIR ?? "E:/videos-vendapps";

/**
 * Arquivo de sessão salvo pelo globalSetup.
 * Contém cookies + localStorage após login bem-sucedido.
 */
const AUTH_STATE = path.join(__dirname, "auth", "storage-state.json");

export default defineConfig({
  testDir: "./tests",

  // Execução sequencial — cada teste gera um vídeo limpo
  fullyParallel: false,
  workers: 1,

  // Timeout generoso: tutoriais têm pausas visuais intencionais
  timeout: 120_000,
  expect: { timeout: 15_000 },

  retries: 0,

  // Mantém vídeos e artefatos mesmo quando o teste passa.
  // Padrão do Playwright é 'failures-only', que apaga vídeos de testes bem-sucedidos.
  preserveOutput: "always",

  reporter: [
    ["list"],
    ["html", { outputFolder: "reports", open: "never" }],
    // Organiza vídeos em E:/videos-vendapps/{slug}/{slug}.webm
    // Nunca sobrescreve — arquiva versões anteriores com sufixo de data.
    ["./video-organizer.ts"],
  ],

  // Pasta de saída temporária dos artefatos brutos do Playwright.
  // O video-organizer move os vídeos finalizados para E:/videos-vendapps/.
  outputDir: "./test-output",

  // Login realizado uma única vez antes de todos os testes
  globalSetup: "./auth/setup.ts",

  use: {
    baseURL: "https://novaempresa.vendapps.com.br",

    // Viewport HD — qualidade de tutorial
    viewport: { width: 1280, height: 720 },

    // Localização brasileira
    locale: "pt-BR",
    timezoneId: "America/Sao_Paulo",

    // Sessão autenticada (gerada pelo globalSetup)
    storageState: AUTH_STATE,

    // Gravação de vídeo em todos os testes
    video: {
      mode: "on",
      size: { width: 1280, height: 720 },
    },

    // Screenshots apenas em falhas
    screenshot: "only-on-failure",

    // Trace apenas em falhas (economiza espaço)
    trace: "retain-on-failure",
  },

  projects: [
    // Único projeto: tutoriais com slowMo para vídeo natural
    {
      name: "tutoriais",
      use: {
        ...devices["Desktop Chrome"],
        launchOptions: {
          // 600ms entre cada ação — torna o vídeo legível para o espectador
          slowMo: 600,
        },
      },
    },
  ],
});
