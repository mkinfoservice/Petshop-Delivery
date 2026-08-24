# Handoff — Contexto para Continuidade (2026-08-24)

> Este documento existe para transferir contexto de trabalho para outra sessão/IA que
> vá continuar o desenvolvimento do vendApps. Não é documentação de produto (isso é o
> `README.md`) — é um resumo do **estado real do código, decisões tomadas e por quê,
> armadilhas já descobertas, e o que falta fazer**, escrito por quem acabou de
> implementar essas partes.

---

## 1. O projeto em uma frase

SaaS multi-tenant de gestão comercial (petshop/varejo/food service) — catálogo público,
PDV, pedidos por canal (site, mesa, telefone, iFood, Mercado Livre), fiscal (NFC-e),
WhatsApp, fidelidade, rotas de entrega. Backend .NET 8 + EF Core em Render, frontend
React+Vite em Vercel, banco PostgreSQL na NeonDB. Ver `README.md` na raiz pra visão de
produto completa — este doc foca no que o README não cobre em profundidade técnica.

---

## 2. Infraestrutura — como acessar e o que saber

- **Backend produção**: `https://vendapps.onrender.com` — deploy automático a cada
  `git push origin main` (Render assiste o repo). Health checks: `/health/live`,
  `/health/ready`. Depois de todo push, **espere o deploy e confirme os dois 200**
  antes de considerar a mudança concluída — várias vezes neste projeto um push quebrou
  o boot em produção (migration mal formada, connection string errada) e só se percebe
  checando o health.
- **Frontend produção**: Vercel, auto-deploy também via push. `VITE_API_URL` aponta pro
  Render.
- **Banco**: NeonDB (Postgres serverless), projeto `vendapps-alpha`
  (`blue-shape-94014787`), branch `production` (`br-quiet-glade-aiw627l6`), banco
  `vendapps` — **atenção**: não é o "neondb" default que ferramentas assumem quando
  você omite o nome do banco; sempre passar `databaseName: "vendapps"` explicitamente.
- **Neon MCP**: se configurado na sessão (`.mcp.json` na raiz, não versionado), dá
  acesso de leitura/escrita direto ao Postgres de produção via `mcp__neon__run_sql` e
  afins. Extremamente útil pra diagnosticar problemas reais em vez de adivinhar —
  usado repetidas vezes nesta sessão pra inspecionar dados, confirmar migrations
  aplicadas, e validar que um fix realmente funcionou. Se não estiver disponível,
  pedir pro usuário autorizar via `/mcp` (só funciona em sessão interativa).
- **Multi-tenant**: subdomínio (`slug.vendapps.com.br`) resolvido em runtime no
  frontend; o **backend nunca vê esse Host** (Render/Vercel são domínios distintos) —
  tenant sempre resolvido via header `X-Tenant-Slug` ou slug explícito no payload,
  nunca por `Request.Host`. Isso já causou confusão mais de uma vez — não assumir que
  dá pra inferir tenant do host do lado do servidor.
- **CI/CD**: `.github/workflows/ci.yml` — build+test backend, build+typecheck frontend.
  `package-lock.json` é gitignored de propósito neste repo — CI usa `npm install`, não
  `npm ci` (que exigiria o lockfile).

---

## 3. Hub de marketplace nativo — decisão e estado atual

### Por que nativo (não third-party)

Cogitou-se inicialmente usar um hub terceiro (Bling/AnyMarket) pra conectar Mercado
Livre/Magalu/Casas Bahia de uma vez. **O usuário reverteu essa decisão** explicitamente:
não quer pagar mensalidade de hub terceiro além do percentual que já perde pros próprios
marketplaces. Decisão final: vendApps constrói seu próprio hub, marketplace por
marketplace, em ordem de complexidade crescente (Mercado Livre → Magalu → Casas Bahia,
sendo Casas Bahia a mais pesada por ter processo de certificação próprio "Certifica").

### Arquitetura reutilizável (já validada com iFood + Mercado Livre)

- `IMarketplaceOrderIngester` — contrato que cada marketplace implementa pra normalizar
  pedido recebido em `Order`/`OrderItem` reais (mesmo modelo de delivery/mesa/telefone,
  sem tabela paralela).
- `MarketplaceIntegration` — 1 linha por tenant×canal. Credenciais (`ClientSecretEncrypted`,
  `AccessTokenEncrypted`, `RefreshTokenEncrypted`) protegidas via
  `MarketplaceCredentialProtectionService` (`IDataProtectionProvider`, padrão idêntico ao
  já usado em `CpfProtectionService`).
- `MarketplaceCatalogSyncMode` (enum: NotConfigured/AllProducts/SelectedCategories/
  SelectedProducts) — **padrão obrigatório pra qualquer marketplace novo**: nunca
  sincroniza o catálogo inteiro sem escolha explícita do lojista. Pedido explícito do
  usuário, generalizado além do Mercado Livre.
- `MarketplaceProductMapping` — vínculo persistido Product↔item externo, com
  `Status`/`LastErrorMessage` por produto (substituiu casamento por
  `InternalCode`/`Barcode` em tempo de execução, que era como o iFood fazia).
- **Camada de resiliência** (construída para Mercado Livre em 2026-08-24, replicar em
  qualquer marketplace novo):
  - `MarketplaceIngestionFailure` — fila de falha reprocessável (sobrevive a
    deploy/restart, diferente do único slot `MarketplaceIntegration.LastErrorMessage`).
  - `ProcessWebhookAsync` **relança exceção** em falha de negócio — sem isso, o retry
    automático padrão do Hangfire (10 tentativas, backoff exponencial) nunca é
    acionado, porque `IngestAsync` engole erros e retorna um `IngestResult` em vez de
    lançar. **Isso é uma armadilha real que já existia e só foi corrigida agora** — ao
    portar pra Magalu/Casas Bahia, replicar esse padrão desde o início.
  - Job recorrente de reconciliação (`MercadoLivreReconciliationJob`, roda de hora em
    hora) — busca pedidos recentes via API e reingesta o que o webhook não entregou.
  - Alerta via `AdminAlert` + `WhatsAppClient.SendTextAsync` pro
    `Company.OwnerAlertPhone` — mesmo padrão já usado em `SupplyAlertService`,
    `ContingencyReprocessJob`, `FiscalCertExpiryAlertJob`. **Reaproveitar esse padrão
    exato**, não inventar um mecanismo de alerta novo.

### Status por marketplace

| Marketplace | Status |
|---|---|
| iFood | Em produção há mais tempo — **tem dívida técnica não paga**: `iFoodStatusCallbackService` implementado e registrado no DI mas **zero call sites** (nunca é chamado quando status do pedido muda no admin/PDV); token OAuth só em memória (`AddSingleton` + `Dictionary`, perde em restart); sem camada de resiliência (a mesma construída pro ML ainda não foi retroaplicada ao iFood). |
| Mercado Livre | **Completo**: OAuth (Authorization Code + refresh), webhook (`orders_v2`, tenant resolvido por `MerchantId` no payload — **uma única URL de notificação por app developer**, diferente do iFood que tem URL por lojista), sync de catálogo com escopo, camada de resiliência inteira. App já criada no painel deles (App ID `5809224096230161`, credenciais no cofre do Render). Pendências menores antes de considerar 100% pronto pra piloto real: validar `listing_type_id="gold_special"` contra `GET /users/{id}/available_listing_types`; decidir tratamento de produto sem GTIN quando categoria exige. |
| Magalu | **Pesquisa feita, zero código escrito.** OAuth Authorization Code (`id.magalu.com/login` → `id.magalu.com/oauth/token`). Webhook: mesmo padrão "notificação rasa" do ML (`{data:{status,params:{id},resource}, tenant_id, topic}`) — tópicos `orders_order` (status new/approved) e `orders_delivery` (status shipped/approved/invoiced/cancelled/delivered). **Hipótese não confirmada**: `tenant_id` no payload sugere uma única URL por app (como ML), mas não está explícito na doc — validar na prática. Scopes identificados: `open:order-order-seller:read`, `open:order-delivery-seller:read` (scope de catálogo ainda não identificado). **Bloqueado no usuário**: precisa criar a aplicação em developers.magalu.com e registrar `https://vendapps.onrender.com/api/integrations/magalu/callback` (OAuth) e `https://vendapps.onrender.com/api/webhooks/magalu/notifications` (webhook), depois passar client_id/secret. **O usuário pediu explicitamente pra aguardar o sinal dele antes de começar a codar isso.** |
| Casas Bahia | Não iniciado. Tem processo de certificação próprio ("Certifica") — mais pesado dos três, ir por último. |

---

## 4. Enriquecimento de catálogo

Módulo pré-existente (nomes + imagens via Cosmos/Bluesoft) documentado em
`docs/enrichment.md` — **esse doc está desatualizado**, não cobre o que foi adicionado
nesta sessão:

- **Geração de descrição via IA** (`ProductDescriptionSuggestion`,
  `ProductDescriptionGeneratorService` chamando Anthropic Messages API, modelo
  `claude-haiku-4-5-20251001`, requer env var `Anthropic__ApiKey` no Render). **Nunca
  auto-aplica** — sempre cai em fila de revisão manual (`Status: Pending/Approved/Rejected`,
  sem `AutoApplied`), decisão deliberada por risco de alucinação em texto de e-commerce
  ao vivo (propaganda enganosa).
- **Fila de revisão de imagem aceita submissão externa**: endpoint
  `POST /admin/enrichment/review/images/submit` permite que uma ferramenta externa
  (não o pipeline nativo Cosmos) empurre uma candidata pra revisão. Usado hoje por
  `scripts/enrich_images_gui.py` — script GUI local (Tkinter) que busca imagem via
  Mercado Livre API + Bing scraping + DuckDuckGo + opcionalmente Google Custom Search
  (se o usuário configurar API key/cx no próprio app, com limite rígido de 100
  buscas/dia codificado pra nunca gerar cobrança excedente). Modo manual do script
  aplica direto (humano já decidiu ao clicar); modo Automático manda pra fila de
  revisão (sem score de confiança, não pode aplicar cego).
- **Usuário decidiu não pagar a ativação de faturamento do Google Cloud (R$50 único)**
  por enquanto — segue fazendo enriquecimento de imagem/descrição manualmente,
  produto por produto. Não sugerir reativar isso a menos que o usuário peça.

---

## 5. Armadilhas técnicas já descobertas (não repetir)

- **EF Core: `enum.ToString() == string` dentro de `.Where()` não traduz pra SQL**
  quando a propriedade tem `.HasConversion<string>()` configurado — lança
  `InvalidOperationException: "could not be translated"`, capturado pelo exception
  handler global e vira 500 genérico em produção (corpo da resposta só tem
  `{error, correlationId}`, sem detalhe — pegar a stack real no log do Render pelo
  `correlationId`). Fix: `Enum.TryParse<T>(status, true, out var parsed)` +
  `.Where(x => x.Status == parsed)`. Esse bug já apareceu 3x copiado (nomes, imagens,
  descrições) — **sempre revisar esse padrão ao escrever filtro por status via query
  string**.
- **Migration sem `[Migration]`/`[DbContext]`**: uma migration antiga
  (`AddMarketplaceIntegration`, abril 2026) nunca teve esses atributos e o EF **nunca a
  reconheceu como válida** — nunca criou a tabela, nunca registrou no
  `__EFMigrationsHistory`, e isso só foi descoberto meses depois quando o deploy
  quebrou. Diagnóstico: `dotnet ef migrations list --no-connect` (lista migrations
  visíveis pro assembly sem precisar de conexão com banco) — se uma migration não
  aparece aí, ela é invisível pro EF mesmo que o arquivo `.cs` exista.
- **Sempre gerar migration via `dotnet ef migrations add`** — nunca escrever uma
  migration à mão; o `AppDbContextModelSnapshot.cs` fica dessincronizado e causa
  `NullReferenceException` esquisito em runtime.
- **Connection string do Neon vem em formato URI** (`postgresql://user:pass@host/db`)
  — Npgsql exige formato chave=valor (`Host=...;Port=...;Database=...;Username=...;
  Password=...;SSL Mode=Require;Trust Server Certificate=true`). Colar a URI direto
  derruba o Render.
- **Deploy durante um Hangfire job em execução cancela o job** (Render reinicia o
  processo, o `CancellationToken` do Hangfire dispara). Se um lote de enriquecimento
  ou sync grande falhar com "The operation was canceled." logo depois de um push, é
  isso — não é bug, é característica de jobs longos coincidindo com deploy. Reexecutar
  resolve (idempotente).
- **`.cs` nunca em Latin-1** — sempre UTF-8. Se aparecer `Ã£` etc. em texto, é
  double-encoding; corrigir com script Python de charset, não regravar manualmente.

---

## 6. Como este projeto gosta de trabalhar (preferências confirmadas do usuário)

- Sempre **buildar backend E frontend** antes de considerar uma mudança pronta
  (`dotnet build`, `npx tsc --noEmit` + `npm run build`).
- Sempre **verificar migration discoverability** (`dotnet ef migrations list --no-connect`)
  depois de gerar uma nova.
- Sempre **esperar o deploy do Render estabilizar** (`/health/live` + `/health/ready`
  em 200 repetido) antes de reportar como concluído — várias vezes nesta sessão um
  push quebrou o boot e só se percebeu checando isso.
- Ao investigar um bug em produção, **usar Neon MCP pra olhar dado real** em vez de só
  ler código e supor — isso já resolveu diagnósticos que teriam sido só suposição
  (ex: descobrir que o problema era `Status.ToString()` não traduzível, não os dados).
- Ao propor algo com trade-off real (custo de API, arquitetura, escopo), **dar
  recomendação + trade-off em poucas frases e perguntar antes de implementar** — não
  assumir e construir sem alinhar quando a decisão afeta dinheiro ou arquitetura de
  longo prazo (ex: Google Custom Search API — o usuário quis entender o custo real
  antes de decidir, e no fim optou por não pagar).
- Usuário lê e testa cada mudança em produção real (não em ambiente de staging) —
  espera que eu confirme saúde do deploy e, quando aplicável, valide dado real via
  Neon MCP antes de dizer "pronto".

---

## 7. Próximo passo imediato

**Aguardando o usuário** criar a aplicação Magalu no portal deles e passar
client_id/secret (ver seção 3, linha "Magalu"). **Não iniciar implementação de código
do Magalu sem esse sinal explícito do usuário** — ele pediu pra aguardar ordem dele.

Quando ele der o sinal: seguir a arquitetura do Mercado Livre como template (ver seção
3), e desta vez **já embutir a camada de resiliência desde o início** (não deixar pra
depois, como aconteceu com o Mercado Livre — a dívida só foi paga numa etapa separada
por pedido explícito do usuário).
