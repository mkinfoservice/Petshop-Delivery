# Como criar um novo fluxo de tutorial

Guia passo a passo para adicionar um novo vídeo à automação.
Siga sempre nesta ordem para manter a consistência.

---

## Passo 1 — Identificar o fluxo

Defina claramente:
- **Módulo:** qual seção do sistema? (ex: Pedidos)
- **Ação:** o que será demonstrado? (ex: Criar pedido de delivery)
- **Número de ordem:** próximo disponível na pasta do módulo (ex: 03)
- **Nome do arquivo:** `03-criar-pedido-delivery`

---

## Passo 2 — Criar o roteiro primeiro

Antes de escrever qualquer código, escreva o roteiro em Markdown.
Isso força você a pensar nas cenas antes de automatizar.

Crie em `automation/roteiros/{modulo}/NN-nome.md`:

```markdown
# Roteiro — NN: Título do fluxo

**Módulo:** NomeModulo
**Duração estimada:** X minutos

## Objetivo do vídeo
...

## Roteiro narrado

### Cena 1 — NomeDaCena (00:00 – 00:30)
**O que aparece na tela:** ...
**Narração sugerida:** > "..."

### Cena 2 — ...

## Checklist de validação
- [ ] ...
```

---

## Passo 3 — Criar a pasta de testes (se for módulo novo)

```bash
mkdir automation/playwright/tests/{modulo}
```

---

## Passo 4 — Criar o arquivo de teste

Crie em `automation/playwright/tests/{modulo}/NN-nome.spec.ts`.

### Template base

```typescript
/**
 * Fluxo: Título do fluxo
 * Módulo: NomeModulo
 * Roteiro: automation/roteiros/{modulo}/NN-nome.md
 */
import { test, expect } from "@playwright/test";
import {
  navegarPara,
  aguardarCarregamento,
  pausaVisual,
  aguardarURL,
} from "../../helpers/navigation";
import {
  preencherPorPlaceholder,
  clicarBotao,
} from "../../helpers/form";

// Dados usados no vídeo (sempre fictícios)
const DADOS = {
  campo1: "Valor exemplo",
} as const;

test.describe("Módulo: NomeModulo", () => {
  test("NN — Título do fluxo", async ({ page }) => {
    // ── Cena 1: Ponto de partida ─────────────────────────────────────────
    await navegarPara(page, "/app");
    await pausaVisual(2_000);

    // ── Cena 2: Navegação ─────────────────────────────────────────────────
    await page.getByRole("link", { name: /nome-do-link/i }).click();
    await aguardarCarregamento(page);
    await pausaVisual(1_500);

    // ── Cena 3: Ação principal ────────────────────────────────────────────
    await preencherPorPlaceholder(page, "placeholder do campo", DADOS.campo1);
    await pausaVisual(800);

    await clicarBotao(page, "Texto do botão");

    // ── Validações ────────────────────────────────────────────────────────
    await aguardarURL(page, /padrao-esperado/);
    await expect(page.getByText(DADOS.campo1)).toBeVisible();

    // Pausa final — espectador vê o resultado
    await pausaVisual(3_000);
  });
});
```

---

## Passo 5 — Regras de pausaVisual()

| Momento | Duração recomendada |
|---|---|
| Após carregar uma página nova | `2_000` (2s) |
| Após preencher um campo | `800` (0,8s) |
| Antes de submeter um formulário | `1_500` (1,5s) |
| Após resultado / redirect | `3_000` (3s) — encerramento |
| Entre seções do formulário | `1_000` (1s) |

---

## Passo 6 — Testar antes de gravar

```bash
cd automation/playwright

# 1. Confirmar que o setup está atual
npm run setup

# 2. Rodar o teste com browser visível para revisar
npx playwright test tests/{modulo}/NN-nome.spec.ts --headed --project=tutoriais

# 3. Se estiver certo, rodar para gravar com slowMo ativado
npm run gravar -- tests/{modulo}/NN-nome.spec.ts
```

---

## Passo 7 — Verificar o vídeo gerado

O vídeo fica em:
```
E:\videos-vendapps\NN-nome-tutoriais\video.webm
```

Abra e revise antes de publicar. Conferir checklist do roteiro.

---

## Passo 8 — Converter para MP4 (opcional)

```bash
ffmpeg -i "E:\videos-vendapps\NN-nome-tutoriais\video.webm" ^
       -c:v libx264 -c:a aac ^
       "E:\videos-vendapps\NN-nome.mp4"
```

---

## Passo 9 — Atualizar o índice de vídeos

Adicione uma linha em `automation/docs/indice-videos.md` (criar se não existir):

```markdown
| NN | Módulo | Título | Arquivo |
| 01 | Clientes | Cadastrar novo cliente | clientes/01-cadastrar-cliente |
| NN | Modulo | Título | modulo/NN-nome |
```

---

## Dicas de qualidade

- **Use dados sempre fictícios** — nunca CPF/telefone real nos vídeos
- **Nomes realistas** — "Ana Paula Rodrigues", não "Teste 123"
- **Pause antes de ações importantes** — espectador precisa de tempo
- **Uma ação por vez** — não preencha dois campos sem pausar
- **Valide SEMPRE no final** — o teste deve confirmar o sucesso
- **Mantenha o slowMo em 600ms** — confortável para leitura no vídeo
