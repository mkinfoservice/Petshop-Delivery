/**
 * Fluxo: Módulo Equipe — usuários do sistema
 * Módulo: Administrativo
 * Vídeo: 01-equipe
 *
 * Roteiro completo em: automation/roteiros/admin/01-equipe.md
 */

import { test } from "@playwright/test";
import {
  navegarPara,
  pausaVisual,
} from "../../helpers/navigation";
import {
  configurarTema,
  injetarUITutorial,
  mostrarLegenda,
  ocultarLegenda,
} from "../../helpers/tutorial";

const LEGENDAS = {
  acessando:
    "O módulo Equipe centraliza os usuários do sistema.\nAcesse pelo menu lateral em Plataforma > Equipe.",
  lista:
    "Aqui estão todos os colaboradores com acesso ao sistema.\nCada usuário tem um papel: Admin, Gerente, Atendente ou Entregador.",
  papeis:
    "O papel define o que cada usuário pode ver e fazer:\nAdmin tem acesso total; Atendente acessa apenas o operacional.",
} as const;

test.describe("Módulo: Administrativo", () => {
  test("01 — Equipe (usuários do sistema)", async ({ page }) => {

    await configurarTema(page);

    // ── Cena 1: Acessar o módulo ─────────────────────────────────────────
    await navegarPara(page, "/app/equipe");
    await injetarUITutorial(page);
    await page.waitForSelector("h1", { state: "visible", timeout: 15_000 });

    await mostrarLegenda(page, LEGENDAS.acessando);
    await pausaVisual(2_500);
    await ocultarLegenda(page);

    // ── Cena 2: Lista de usuários ────────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.lista);
    await pausaVisual(3_000);
    await ocultarLegenda(page);

    // ── Cena 3: Papéis e permissões ──────────────────────────────────────
    await mostrarLegenda(page, LEGENDAS.papeis);
    await pausaVisual(3_000);
    await ocultarLegenda(page);
  });
});
