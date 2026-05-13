# vendApps — Handoff Técnico

> Documento de onboarding para IA ou desenvolvedor. Reflete o estado atual do repositório (**maio 2026**).  
> Leia antes de qualquer tarefa. Atualize quando entregar algo novo.

---

## 1. Visão Geral

**vendApps** é uma plataforma SaaS **multi-tenant** de gestão comercial. Cada empresa recebe subdomínio próprio (`empresa.vendapps.com.br`) com catálogo online, PDV, painel admin, programa de fidelidade, emissão de NFC-e e integração WhatsApp — totalmente isolados por `CompanyId`.

**Repositório:** `https://github.com/mkinfoservice/vendApps`  
**Branch principal:** `main`  
**Backend (prod):** `https://vendapps.onrender.com` (Render — .NET + NeonDB PostgreSQL)  
**Frontend (prod):** Vercel — React SPA; subdomínio detectado em runtime

---

## 2. Stack Técnica

| Camada | Tecnologia |
|---|---|
| Backend | ASP.NET Core .NET 8 + EF Core 8 + PostgreSQL (NeonDB) |
| Frontend | React 18 + TypeScript + Vite + Tailwind CSS + Radix UI (shadcn) |
| Mensageria | MassTransit 8.3 + RabbitMQ (CloudAMQP) + Outbox Pattern (EF Core) |
| Jobs | Hangfire 1.8 + PostgreSQL storage |
| Auth | JWT — roles: `admin`, `gerente`, `atendente`, `deliverer` |
| Realtime | SignalR — impressoras e balanças |
| Deploy | Render (backend auto-deploy via push) + Vercel (frontend) |
| State/Cache | TanStack Query (React Query) v5 |
| Ícones | Lucide React |

---

## 3. Multi-tenancy

- Slug do subdomínio resolvido em runtime em `catalog/api.ts` via `resolveCompanySlug()`
- Todos os dados filtrados por `CompanyId` (nunca cruza entre clientes)
- CORS automático para `*.vendapps.com.br` em `Program.cs:IsVendappsSubdomain()`
- JWT do admin carrega claim `companyId` → todos os controllers usam `Guid.Parse(User.FindFirstValue("companyId")!)`

**Empresas de demo (DbSeeder — idempotente por slug):**

| Slug | ID prefix | Nome |
|---|---|---|
| `petshop-demo` | `11111111-...` | Petshop Demo |
| `suaempresa` | `22222222-...` | Sua Empresa |
| `novaempresa` | `33333333-...` | Empresa Teste |

---

## 4. Estrutura de Pastas (o que importa)

```
vendApps/
├── backend/Petshop.Api/
│   ├── Controllers/
│   │   ├── OrdersController.cs           Pedidos delivery — publica eventos MassTransit
│   │   ├── PdvController.cs              PDV — venda, itens, pagamento, fidelidade
│   │   ├── CatalogController.cs          GET /catalog/{slug}/products|categories
│   │   ├── WhatsAppWebhookController.cs  Webhook Meta/Evolution
│   │   └── FiscalAdminController.cs      NFC-e manual / reprocessamento
│   ├── Data/
│   │   ├── AppDbContext.cs               30+ DbSets + outbox EF entities
│   │   ├── DbSeeder.cs                   seed de empresas e catálogo
│   │   ├── AddonGroupSeeder.cs           classifica addons em grupos na inicialização
│   │   └── AddonSplitSeeder.cs           desmembra addons combinados ("A ou B" → A + B)
│   ├── Entities/
│   │   ├── Customers/Customer.cs         Phone, CpfHash, PointsBalance
│   │   ├── Pdv/SaleOrder.cs              venda PDV com CustomerId, CustomerPhone
│   │   └── WhatsApp/WhatsAppMessageLog.cs TriggerStatus para idempotência
│   ├── Messaging/
│   │   ├── Configuration/MessagingSetup.cs  AddMassTransit + Outbox config
│   │   ├── Contracts/                       eventos (interfaces)
│   │   └── Consumers/                       5 consumers registrados
│   ├── Services/
│   │   ├── WhatsApp/WhatsAppNotificationService.cs  toda lógica de notificação WA
│   │   ├── Customers/LoyaltyService.cs              EarnAsync, EarnForOrderAsync, RedeemAsync
│   │   └── Fiscal/Jobs/FiscalQueueProcessorJob.cs   NFC-e assíncrona (Hangfire)
│   ├── Migrations/                        migrações EF Core
│   └── Program.cs                         startup + safety nets SQL idempotentes
│
└── frontend/petshop-web/src/
    ├── features/
    │   ├── catalog/
    │   │   ├── api.ts                     fetchProducts + normalizeProductGroups()
    │   │   ├── ProductAddonStepper.tsx    UI step-by-step de adicionais
    │   │   ├── useProductStepper.ts       hook — estado, validação, buildSynthetic()
    │   │   └── ProductQuickViewModal.tsx  modal desktop
    │   ├── cart/
    │   │   ├── cart.tsx                   CartProvider + useCart (NÃO alterar)
    │   │   └── CartSheet.tsx / CartSidebar.tsx
    │   └── pdv/
    │       ├── api.ts                     addItem, paySale, searchCustomer…
    │       └── PdvContext.tsx
    ├── pages/
    │   ├── pdv/PdvPage.tsx                PDV GoCoffee (AddonModal com stepper)
    │   ├── Checkout.tsx
    │   └── admin/
    │       ├── ProductForm.tsx            CRUD produto + adicionais
    │       └── PromotionsPage.tsx         sistema de cupons
    └── components/
        └── Toast.tsx                      ToastProvider + useToast
```

---

## 5. Arquitetura de Mensageria (MassTransit — maio 2026)

### Infraestrutura

- **Produção:** CloudAMQP (RabbitMQ) via `RabbitMq__Uri` (URL amqps completa)
- **Dev local:** bus in-memory — `RabbitMq__Enabled=false` em `appsettings.Development.json`
- **Outbox Pattern:** eventos gravados em `OutboxMessage` na mesma transação do `SaveChangesAsync`; poller MassTransit entrega ao broker em background
- Configuração: `backend/Petshop.Api/Messaging/Configuration/MessagingSetup.cs`

### Eventos e Consumers

| Evento | Publicado em | Consumer(s) |
|---|---|---|
| `GeocodingRequestedEvent` | `OrdersController.UpdateStatus` (PRONTO_PARA_ENTREGA, sem coords) | `GeocodingRequestedConsumer` |
| `WhatsAppNotificationRequestedEvent` | `OrdersController.UpdateStatus` + `Create` | `WhatsAppNotificationConsumer` |
| `OrderDeliveredEvent` | `OrdersController.UpdateStatus` (ENTREGUE) | `DavCreationConsumer`, `LoyaltyEarnConsumer` |
| `PdvWhatsAppNotificationRequestedEvent` | `FiscalQueueProcessorJob` (após NFC-e autorizada) | `PdvWhatsAppNotificationConsumer` |

### Regra crítica — ordem de operações

```csharp
// CORRETO: Publish() ANTES de SaveChangesAsync()
// O evento vai para OutboxMessage na mesma transação → zero-loss
await _publisher.Publish(new WhatsAppNotificationRequestedEvent { ... }, ct);
await _db.SaveChangesAsync(ct);  // commit atômico: mudança de estado + OutboxMessage

// ERRADO: SaveChanges antes, depois Publish com try-catch
// ❌ Processo pode cair entre os dois — evento perdido
```

**Nunca envolver `Publish()` em try-catch** — o broker não é contactado neste ponto. Falha de RabbitMQ não impacta o request HTTP.

### Tabelas do Outbox (criadas em `AddMassTransitOutbox`)

| Tabela | Finalidade |
|---|---|
| `OutboxMessage` | Mensagens aguardando entrega ao broker |
| `OutboxState` | Estado do outbox por DbContext scope |
| `InboxState` | FK necessária por OutboxMessage (idempotência consumer-side) |

---

## 6. WhatsApp — Notificações e Idempotência

Todas as notificações são registradas em `WhatsAppMessageLogs` com `TriggerStatus`. Antes de enviar, verifica se já existe log com mesmo `TriggerStatus + OrderId/SaleId`. Isso garante que cada evento gera **exatamente uma** mensagem.

**Triggers ativos:**

| TriggerStatus | Evento |
|---|---|
| `PRONTO_PARA_ENTREGA` | Pedido pronto para entrega |
| `SAIU_PARA_ENTREGA` | Pedido saiu para entrega |
| `ENTREGUE` | Pedido entregue |
| `ENTREGUE_LOYALTY_COMPLEMENT` | Pontos de fidelidade pós-delivery |
| `PDV_LOYALTY_COMPLEMENT` | Pontos de fidelidade pós-PDV |
| `SALE_COMPLETED` | NFC-e autorizada (comprovante PDV) |

**Bug corrigido (maio 2026, commit `b72defb`):** loyalty PDV enviava 2x porque `PDV_LOYALTY_COMPLEMENT` e `ENTREGUE_LOYALTY_COMPLEMENT` são triggers distintos. Fix: `NotifySaleCompletedAsync` agora faz cross-check — se `PDV_LOYALTY_COMPLEMENT` já existe para o `saleId`, não envia o espelho delivery.

### Configurações WhatsApp (appsettings)

```json
{
  "WhatsApp": {
    "LoyaltyComplement": {
      "Enabled": true,
      "DelaySeconds": 2,
      "SendWhenPointsZero": false,
      "TemplateName": "card_transaction_alert_2"
    }
  }
}
```

---

## 7. Programa de Fidelidade

### Fluxo PDV (`PdvController.Pay`)

1. Após `tx.CommitAsync()`, roda `EarnAsync` **antes** de enfileirar `FiscalQueueProcessorJob`
2. Identidade confirmada por CPF hash **ou** por cliente buscado via telefone (sem CPF digitado)
3. Enfileira `SendPdvLoyaltyComplementAsync` para qualquer cliente confirmado (independente de NFC-e)

### Fluxo Delivery (`LoyaltyEarnConsumer`)

- Consumer processa `OrderDeliveredEvent` de forma assíncrona
- `LoyaltyTransaction.OrderId` salvo → permite lookup no complemento WhatsApp delivery

### `SendPdvLoyaltyComplementAsync(saleId)`

- Template `card_transaction_alert_2`: `{{1}}`=firstName, `{{2}}`=earnedPoints, `{{3}}`=pointsBalance
- Fallback de telefone: usa `sale.CustomerPhone` ou `Customer.Phone`
- Idempotência dupla: verifica `PDV_LOYALTY_COMPLEMENT` + `ENTREGUE_LOYALTY_COMPLEMENT`

---

## 8. Fiscal — NFC-e

- `FiscalQueueProcessorJob` processa fila assíncrona via Hangfire
- Após autorização SEFAZ: publica `PdvWhatsAppNotificationRequestedEvent` **antes** de `SaveChangesAsync`
- Consumer `PdvWhatsAppNotificationConsumer` chama `NotifySaleCompletedAsync` → comprovante + fidelidade

---

## 9. EF Core — Migrações

**Regra crítica:** nunca criar arquivo de migração manualmente sem atualizar o snapshot.

O snapshot `AppDbContextModelSnapshot.cs` só é atualizado quando se roda `dotnet ef migrations add`. Migrações criadas manualmente (sem `.Designer.cs`) deixam o snapshot desatualizado e causam `NullReferenceException` em `MigrationsModelDiffer.Initialize` na próxima vez que você tentar gerar uma migração.

**Fix se o snapshot ficar corrompido:**
```bash
# 1. Deletar o snapshot
rm Migrations/AppDbContextModelSnapshot.cs

# 2. Gerar snapshot limpo (ignora a migração temporária)
dotnet ef migrations add TempSnapshot

# 3. Deletar os arquivos da migração temporária (manter só o snapshot)
rm Migrations/2026*_TempSnapshot.cs
rm Migrations/2026*_TempSnapshot.Designer.cs

# 4. Gerar a migração real
dotnet ef migrations add NomeDaMigracao
```

Migrações com `migrationBuilder.Sql()` são válidas para operações idempotentes (`IF NOT EXISTS`), mas devem ser criadas via `dotnet ef migrations add` primeiro (vazio) e depois preenchidas manualmente — para garantir que o snapshot seja atualizado.

---

## 10. Módulo de Adicionais (Addon Groups + Stepper)

### Entidades

**`ProductAddonGroup`** — grupo de opções de um produto:
- `SelectionType`: `"single"` (radio, auto-avança no stepper) ou `"multiple"` (checkbox)
- `IsRequired`, `MinSelections`, `MaxSelections`, `SortOrder`

**`ProductAddon`** — item dentro de um grupo:
- `AddonGroupId` — FK para o grupo (nulo = avulso legacy)
- `IsDefault` — pré-selecionado ao abrir o stepper

### Seeders de inicialização (idempotentes)

1. **`AddonGroupSeeder`** — classifica addons em grupos; marca `IsDefault = true` no primeiro do Tipo de Leite
2. **`AddonSplitSeeder`** — detecta addons com `" ou "` no nome e divide em registros individuais

### Frontend — `normalizeProductGroups()`

Em `catalog/api.ts`: se o produto retornar sem grupos mas com addons flat, cria grupos sintéticos em memória com a mesma lógica de classificação. Garante que o stepper apareça mesmo sem restart do servidor.

### Regra crítica — `buildSynthetic()` em `useProductStepper.ts`

Codifica addon IDs no `synthetic.id` como `{productId}__{id1_id2_...}`. O PDV depende desse formato para extrair IDs ao chamar `addItem`:

```typescript
// PdvPage.tsx — handleStepperConfirm
const addonIds = synthetic.id.split("__")[1].split("_");
```

**Nunca alterar esse formato sem atualizar os dois lados.**

---

## 11. Banco de Dados — Safety Nets

Safety nets SQL idempotentes em `Program.cs` garantem colunas mesmo se migration foi pulada:

```sql
ALTER TABLE "ProductAddons"
    ADD COLUMN IF NOT EXISTS "AddonGroupId" uuid,
    ADD COLUMN IF NOT EXISTS "IsDefault" boolean NOT NULL DEFAULT false;

ALTER TABLE "LoyaltyTransactions"
    ADD COLUMN IF NOT EXISTS "OrderId" uuid;
```

**Tabelas principais:**

| Tabela | Notas |
|---|---|
| `ProductAddons` | `AddonGroupId` (FK), `IsDefault`, `IsActive` |
| `ProductAddonGroups` | `ProductId`, `SelectionType`, `IsRequired`, `SortOrder` |
| `LoyaltyTransactions` | `SaleOrderId` (PDV), `OrderId` (delivery) |
| `WhatsAppMessageLogs` | `TriggerStatus` para idempotência de notificações |
| `OutboxMessage` | Mensagens MassTransit aguardando entrega ao broker |
| `OutboxState` | Estado do outbox |
| `InboxState` | Idempotência consumer-side (FK de OutboxMessage) |

---

## 12. Impressão Mobile (Android + iPad)

| Camada | Plataforma | Mecanismo |
|---|---|---|
| `PrintAgent` (Windows service) | PC Windows | `System.Drawing.Printing` → impressora local |
| Print Station (browser) | Qualquer browser | `window.print()` → dialog do SO |
| Mobile Agent (browser) | Android/iPad | Web Bluetooth (ESC/POS) ou `window.print()` + AirPrint |

Todos conectados ao hub SignalR `/hubs/print`.

Arquivos frontend: `features/admin/print/escpos.ts`, `mobilePrint.ts`, `pages/admin/MobilePrintAgentPage.tsx`.

Configurações persistem em `localStorage` por dispositivo (`vendapps_mobile_agent`, `vendapps_mobile_mode`, `vendapps_mobile_paper`).

---

## 13. Sistema de Cupons

Implementado e funcional. Não reimplantar.

- Backend: `Controllers/PromotionController.cs`
- Frontend: `pages/admin/PromotionsPage.tsx`, `features/promotions/promotionsApi.ts`
- Integrado em `PdvController.cs` e `Checkout.tsx`

---

## 14. Padrões de Código

### Backend — Regras de ouro

- **Loyalty e WhatsApp nunca derrubam a venda:** lógica secundária em try/catch isolado fora da transação principal (ou via consumer MassTransit assíncrono)
- **Publish() sempre antes de SaveChangesAsync()** — padrão Outbox Pattern
- Safety nets SQL idempotentes em `Program.cs` como última linha de defesa

### Frontend — Regras críticas (NÃO alterar sem análise)

- `cart.tsx` — CartProvider e `useCart`: contrato fixo
- `catalog/api.ts` — tipos `Product`, `ProductAddon`, `ProductAddonGroup`: alterações exigem atualizar backend e stepper
- `useProductStepper.ts` — `buildSynthetic()`: formato de ID codificado; PDV depende disso

### Frontend — Padrão de chamada admin

```typescript
import { adminFetch } from "@/features/admin/auth/adminFetch";
const data = await adminFetch<T>(`/admin/endpoint`);
```

### Feature flags por tenant

- Gerenciadas via `PlanFeatureService` + `CompanyFeatureOverrides`
- Configuradas no Master Admin → Company → Feature Flags
- Exemplo ativo: `modern_catalog_experience` (catálogo moderno)
- **Ao criar nova API/config que precise de UI: sempre criar a tela correspondente na mesma entrega**

---

## 15. Variáveis de Ambiente

**Backend (Render):**
```
ConnectionStrings__Default=postgresql://...
Jwt__Key=...
Jwt__Issuer=vendapps
Jwt__Audience=vendapps
Jwt__AdminUser=admin
Jwt__AdminPassword=...
Jwt__CompanyId=...
ENABLE_SWAGGER=false

# MassTransit / RabbitMQ (CloudAMQP)
RabbitMq__Enabled=true
RabbitMq__Uri=amqps://user:pass@host/vhost
RabbitMq__QueuePrefix=vendapps

# WhatsApp
WhatsApp__LoyaltyComplement__Enabled=true
WhatsApp__LoyaltyComplement__TemplateName=card_transaction_alert_2
WhatsApp__LoyaltyComplement__DelaySeconds=2
WhatsApp__LoyaltyComplement__SendWhenPointsZero=false
```

**Frontend (Vercel):**
```
VITE_API_URL=https://vendapps.onrender.com
```

---

## 16. Como Rodar Localmente

```bash
# Backend
cd backend/Petshop.Api
# criar appsettings.Development.json com ConnectionStrings__Default
# RabbitMq__Enabled=false (in-memory, sem RabbitMQ local)
dotnet run
# API: http://localhost:5082 | Swagger: /swagger

# Frontend
cd frontend/petshop-web
npm install
# criar .env.local: VITE_API_URL=http://localhost:5082
npm run dev
# http://localhost:5173
```

Na primeira execução: `DbSeeder` + `AddonGroupSeeder` + `AddonSplitSeeder` rodam automaticamente.

---

## 17. Commits Relevantes (maio 2026)

| Hash | Descrição |
|---|---|
| `a907cda` | docs: atualiza README com mensageria MassTransit e Outbox Pattern |
| `40abe99` | feat: etapa 6 — Outbox Pattern com MassTransit EF Core |
| `b72defb` | fix: evita duplicata de mensagem de fidelidade PDV + espelho |
| `f4c4042` | feat: etapa 8 — migra WhatsApp PDV/fiscal do Hangfire para MassTransit |
| `6554d87` | feat: adiciona OrderDeliveredEvent com consumers de DAV e fidelidade |
| `217956d` | fix: protege fluxo principal contra falha de conexão ao RabbitMQ |
| `4fe48e8` | feat: adiciona infraestrutura de mensageria com MassTransit/RabbitMQ |

---

## 18. O que está Funcionando em Produção

- [x] Catálogo online (moderno e legado) por tenant
- [x] Autoatendimento por mesa / QR Code
- [x] PDV com adicionais step-by-step, variantes, balança
- [x] Impressão automática (PrintAgent Windows + Mobile Android/iPad)
- [x] Integração iFood (webhook + sync de cardápio)
- [x] WhatsApp — notificações delivery e PDV (via MassTransit consumers)
- [x] Fidelidade — PDV e delivery, com complemento WhatsApp
- [x] Fiscal NFC-e — emissão automática + contingência + reprocessamento
- [x] DAV / Orçamentos — geração automática pós-entrega
- [x] Rotas de entrega + App do entregador (PWA)
- [x] Estoque, Compras, Financeiro, Agenda, Comissões
- [x] Enriquecimento de catálogo (Cosmos/Bluesoft)
- [x] Sistema de cupons
- [x] Mensageria assíncrona com Outbox Pattern (zero-loss garantido)

---

*Atualizado em: maio 2026*
