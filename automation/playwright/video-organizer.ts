/**
 * video-organizer.ts
 *
 * Reporter Playwright que organiza os vídeos gravados em pastas nomeadas.
 *
 * Destino: E:/videos-vendapps/{slug}/{slug}.webm
 *
 * Se o arquivo já existir, arquiva com sufixo de data/hora:
 *   {slug}_2026-04-16_14-30.webm
 *
 * Isso garante que reruns NUNCA sobrescrevam vídeos anteriores —
 * cada gravação acumula no acervo.
 */

import type { Reporter, TestCase, TestResult } from "@playwright/test/reporter";
import * as fs from "fs";
import * as path from "path";

const VIDEOS_DIR = path.resolve("E:/videos-vendapps");

/**
 * Converte o título do teste num slug de arquivo.
 * "02 — Criar pedido" → "criar_pedido"
 * "06 — Visualizar detalhes do pedido" → "visualizar_detalhes_do_pedido"
 */
function toSlug(title: string): string {
  return title
    .replace(/^\d+\s*[—–-]+\s*/u, "")  // remove "02 — " prefix
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")    // remove acentos
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "_")
    .replace(/^_+|_+$/g, "");
}

class VideoOrganizer implements Reporter {
  onTestEnd(test: TestCase, result: TestResult): void {
    const video = result.attachments.find(
      (a) => a.name === "video" && a.path
    );
    if (!video?.path || !fs.existsSync(video.path)) return;

    const slug = toSlug(test.title);
    if (!slug) return;

    const destDir = path.join(VIDEOS_DIR, slug);
    fs.mkdirSync(destDir, { recursive: true });

    const primaryPath = path.join(destDir, `${slug}.webm`);
    let destPath = primaryPath;

    if (fs.existsSync(primaryPath)) {
      // Arquiva versão anterior com timestamp
      const ts = new Date()
        .toISOString()
        .slice(0, 16)
        .replace("T", "_")
        .replace(":", "-");
      destPath = path.join(destDir, `${slug}_${ts}.webm`);
    }

    try {
      fs.renameSync(video.path, destPath);
    } catch {
      // renameSync falha entre dispositivos diferentes (cross-device)
      fs.copyFileSync(video.path, destPath);
      fs.unlinkSync(video.path);
    }
  }
}

export default VideoOrganizer;
