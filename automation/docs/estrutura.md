# Estrutura da automação — vendApps

## Visão geral

```
automation/
├── playwright/                   # Projeto Playwright (npm)
│   ├── auth/
│   │   ├── credenciais.ts        # usuário/senha/URL (suporta env vars)
│   │   ├── login.ts              # helper: realizarLogin(), sessaoAtiva()
│   │   ├── setup.ts              # roda 1x antes dos testes → salva sessão
│   │   └── storage-state.json    # gerado automaticamente (não commitar)
│   ├── helpers/
│   │   ├── navigation.ts         # navegarPara, aguardarCarregamento, pausaVisual
│   │   └── form.ts               # preencherPorPlaceholder, clicarBotao
│   ├── tests/
│   │   └── {modulo}/             # pasta por módulo do sistema
│   │       └── NN-nome.spec.ts   # NN = número de ordem (01, 02, ...)
│   ├── playwright.config.ts      # configuração central
│   ├── package.json
│   └── tsconfig.json
│
├── roteiros/                     # Roteiros de narração (Markdown)
│   └── {modulo}/
│       └── NN-nome.md
│
├── videos/
│   └── README.md                 # Aponta para E:\videos-vendapps
│
└── docs/
    ├── estrutura.md              # Este arquivo
    └── como-criar-novo-fluxo.md  # Guia passo a passo
```

---

## Convenção de nomes

| Elemento | Padrão | Exemplo |
|---|---|---|
| Pasta de módulo | kebab-case | `clientes`, `pedidos`, `pdv` |
| Arquivo de teste | `NN-acao-objeto.spec.ts` | `01-cadastrar-cliente.spec.ts` |
| Roteiro | `NN-acao-objeto.md` | `01-cadastrar-cliente.md` |
| `test.describe` | `Módulo: NomeModulo` | `Módulo: Clientes` |
| `test()` | `NN — Descrição do fluxo` | `01 — Cadastrar novo cliente` |

---

## Módulos mapeados

| Módulo | Pasta | Rota base |
|---|---|---|
| Clientes | `tests/clientes/` | `/app/atendimento/clientes` |
| Pedidos | `tests/pedidos/` | `/app/pedidos` |
| PDV | `tests/pdv/` | `/app/pdv` |
| Catálogo | `tests/catalogo/` | `/app/catalogo` |
| Atendimento | `tests/atendimento/` | `/app/atendimento` |
| Fiscal/NFC-e | `tests/fiscal/` | `/app/fiscal` |
| Financeiro | `tests/financeiro/` | `/app/financeiro` |
| Configurações | `tests/configuracoes/` | `/app/configuracoes` |

---

## Fluxo de execução

```
npm run setup          → faz login, salva storage-state.json
npm run test           → roda todos os testes sem interface
npm run test:headed    → roda com browser visível (bom para debug)
npm run gravar         → alias de test:headed (semântico para gravação)
```

---

## Onde ficam os vídeos

Os vídeos são salvos em `E:\videos-vendapps\` organizados por pasta de teste:

```
E:\videos-vendapps\
└── 01-cadastrar-cliente-tutoriais\
    └── video.webm
```

O formato é `.webm` (codec VP8). Para converter para `.mp4`:
```bash
ffmpeg -i video.webm -c:v libx264 -c:a aac output.mp4
```

---

## Segurança

- `auth/storage-state.json` contém tokens de sessão — **nunca commitar**
- Está no `.gitignore` por padrão
- `auth/credenciais.ts` lê de variáveis de ambiente em produção/CI
