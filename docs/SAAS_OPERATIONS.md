# vendApps — Guia de Operações SaaS

Documentação interna para criação e gestão de tenants, domínios, planos, feature flags e branding.

---

## Índice

1. [Arquitetura Multi-Tenant](#1-arquitetura-multi-tenant)
2. [Onboarding de Tenant](#2-onboarding-de-tenant)
3. [Planos](#3-planos)
4. [Feature Flags](#4-feature-flags)
5. [Domínio Próprio](#5-domínio-próprio)
6. [Branding](#6-branding)
7. [Variáveis de Ambiente](#7-variáveis-de-ambiente)
8. [Fluxo de Resolução de Tenant](#8-fluxo-de-resolução-de-tenant)
9. [Operações SQL Comuns](#9-operações-sql-comuns)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. Arquitetura Multi-Tenant

O vendApps usa **subdomínio como identidade do tenant**. Cada empresa acessada em `slug.vendapps.com.br` é um tenant isolado. Não existe separação de banco — todos os dados coexistem numa única base PostgreSQL particionada por `CompanyId`.

```
┌─────────────────────────────────────────────────────────────┐
│  slug.vendapps.com.br  OU  pedidos.sualoja.com.br           │
│                    (subdomínio ou domínio próprio)          │
└──────────────────────────┬──────────────────────────────────┘
                           │
              ┌────────────▼────────────┐
              │  TenantResolverService  │ (backend)
              │  1. slug → Companies   │
              │  2. host → CustomDomain│
              └────────────┬────────────┘
                           │
              ┌────────────▼────────────┐
              │     Company entity      │
              │  + PlanFeatureService   │
              │  + StoreFrontConfig     │
              └─────────────────────────┘
```

**Isolamento por tenant:** cada endpoint autenticado valida que o `companyId` do JWT corresponde ao subdomínio requisitado (`TenantHostValidationMiddleware`). Um token de empresa A retorna 403 em qualquer rota de empresa B.

**Slugs reservados** (não podem ser usados por tenants): `www`, `app`, `admin`, `api`, `master`, `suporte`, `blog`, `help`, `status`.

---

## 2. Onboarding de Tenant

### 2.1 Tenants de Demo / Dev (DbSeeder)

O `DbSeeder.cs` cria três tenants na inicialização. São idempotentes — executam toda vez que a API sobe, mas usam `upsert` por slug. **IDs são fixos e nunca devem ser alterados.**

| Tenant | Slug | ID | Segmento | Uso |
|---|---|---|---|---|
| Petshop Demo | `petshop-demo` | `11111111-0000-0000-0000-000000000001` | petshop | Dev / QA |
| Sua Empresa | `suaempresa` | `22222222-0000-0000-0000-000000000002` | petshop | Demo para clientes |
| Go Coffee | `novaempresa` | `33333333-0000-0000-0000-000000000003` | cafeteria | Demo alternativo |

> Os tenants de demo são criados com `Plan = "trial"` e podem ter feature overrides aplicados no seeder.

### 2.2 Criação de Tenant Real (produção)

Tenants reais são criados via painel Master (`/master/companies`) ou diretamente via SQL se necessário.

**Campos obrigatórios:**

| Campo | Tipo | Descrição | Exemplo |
|---|---|---|---|
| `Id` | `uuid` | UUID v4 gerado aleatoriamente | `gen_random_uuid()` |
| `Name` | `varchar(120)` | Nome comercial da empresa | `"Petshop do João"` |
| `Slug` | `varchar(80)` | Identificador URL-safe, único | `"petshop-joao"` |
| `Segment` | `varchar(80)` | Segmento do negócio | `"petshop"` \| `"cafeteria"` \| `"restaurante"` |
| `IsActive` | `bool` | Ativo no sistema | `true` |
| `IsDeleted` | `bool` | Soft-delete | `false` |
| `Plan` | `varchar(30)` | Plano contratado | `"trial"` |
| `WhatsappMode` | `varchar(20)` | Modo WhatsApp | `"own"` \| `"platform"` \| `"none"` |
| `CreatedAtUtc` | `timestamp` | Data de criação | `now()` |

**Campos opcionais relevantes:**

| Campo | Descrição |
|---|---|
| `SuspendedAtUtc` | Quando preenchido, bloqueia acesso público com HTTP 403 |
| `SuspendedReason` | Mensagem interna do motivo da suspensão |
| `PlanExpiresAtUtc` | Data de expiração do plano (não enforced automaticamente — requer cron externo) |
| `OwnerAlertPhone` | Telefone para alertas internos (formato E.164) |
| `SettingsJson` | JSON livre para configurações extras por tenant |

**Regra de slug:**
- Somente letras minúsculas, números e hífens
- Entre 3 e 63 caracteres
- Regex: `^[a-z0-9-]{3,63}$`

**SQL de criação:**
```sql
INSERT INTO "Companies" (
  "Id", "Name", "Slug", "Segment",
  "IsActive", "IsDeleted", "Plan", "WhatsappMode", "CreatedAtUtc"
) VALUES (
  gen_random_uuid(),
  'Nome da Empresa',
  'slug-da-empresa',
  'petshop',
  true, false,
  'trial',
  'own',
  now()
);
```

### 2.3 Checklist pós-criação

- [ ] Empresa criada em `Companies`
- [ ] `StoreFrontConfig` criado (branding padrão) — ver seção [Branding](#6-branding)
- [ ] Plano definido corretamente
- [ ] Feature overrides aplicados se o plano-padrão não for suficiente
- [ ] Subdomínio testado: `https://slug.vendapps.com.br` responde corretamente
- [ ] Credenciais de admin criadas para o tenant (`/auth/login`)

---

## 3. Planos

O sistema tem quatro planos com hierarquia crescente de features:

```
trial (1) < starter (2) < pro (3) < enterprise (4)
```

### Defaults de features por plano

| Feature | trial | starter | pro | enterprise |
|---|:---:|:---:|:---:|:---:|
| `commissions` | ✅ | ✅ | ✅ | ✅ |
| `tips` | ✅ | ✅ | ✅ | ✅ |
| `dav_menu` | ✅ | ✅ | ✅ | ✅ |
| `own_delivery` | ✅ | ✅ | ✅ | ✅ |
| `financial_menu` | ✅ | ✅ | ✅ | ✅ |
| `loyalty_program` | ✅ | ✅ | ✅ | ✅ |
| `agenda` | ❌ | ❌ | ✅ | ✅ |
| `accounting_email_dispatch` | ❌ | ❌ | ✅ | ✅ |
| `modern_catalog_experience` | ❌* | ❌* | ❌* | ❌* |
| `customer_address_geocoding` | ❌* | ❌* | ❌* | ❌* |

> `❌*` = requer opt-in explícito via override, independente do plano (`RequiresExplicitOptIn: true`).

### Alterar plano de um tenant

```sql
UPDATE "Companies"
SET "Plan" = 'pro', "PlanExpiresAtUtc" = '2026-12-31 23:59:59+00'
WHERE "Slug" = 'slug-da-empresa';
```

O cache de features por tenant expira automaticamente em 5 minutos após a alteração.

---

## 4. Feature Flags

Feature flags controlam quais módulos e funcionalidades cada tenant enxerga. Há dois níveis: **default por plano** (automático) e **override por empresa** (manual, via `CompanyFeatureOverrides`).

### 4.1 Catálogo de Feature Keys

| Key | Módulo afetado | Descrição |
|---|---|---|
| `agenda` | Agenda | Agendamentos de serviços (banho & tosa, veterinário) |
| `commissions` | Comissões | Cálculo e distribuição de comissões por vendedor |
| `tips` | Comissões | Pool de gorjetas por sessão |
| `dav_menu` | Orçamento/DAV | Criação de orçamentos e conversão em pedido |
| `financial_menu` | Financeiro | Tela de lançamentos e fluxo de caixa — **inativa** |
| `loyalty_program` | Fidelidade | Programa de pontos, resgates e comunicações |
| `accounting_email_dispatch` | Contabilidade | Fechamento automático e envio ao contador |
| `own_delivery` | Logística | Rotas, entregadores e rastreamento próprio |
| `modern_catalog_experience` | Catálogo | Layout moderno do catálogo (opt-in) |
| `customer_address_geocoding` | Atendimento | Geocoding de endereço no cadastro de cliente (opt-in) |

### 4.2 Aplicar override para um tenant

**Habilitar uma feature que não é default do plano:**
```sql
INSERT INTO "CompanyFeatureOverrides" ("Id", "CompanyId", "FeatureKey", "IsEnabled", "UpdatedAtUtc")
VALUES (
  gen_random_uuid(),
  (SELECT "Id" FROM "Companies" WHERE "Slug" = 'slug-da-empresa'),
  'agenda',
  true,
  now()
)
ON CONFLICT ("CompanyId", "FeatureKey")
DO UPDATE SET "IsEnabled" = true, "UpdatedAtUtc" = now();
```

**Desabilitar uma feature que seria default do plano:**
```sql
INSERT INTO "CompanyFeatureOverrides" ("Id", "CompanyId", "FeatureKey", "IsEnabled", "UpdatedAtUtc")
VALUES (
  gen_random_uuid(),
  (SELECT "Id" FROM "Companies" WHERE "Slug" = 'slug-da-empresa'),
  'own_delivery',
  false,
  now()
)
ON CONFLICT ("CompanyId", "FeatureKey")
DO UPDATE SET "IsEnabled" = false, "UpdatedAtUtc" = now();
```

**Remover override (volta ao default do plano):**
```sql
DELETE FROM "CompanyFeatureOverrides"
WHERE "CompanyId" = (SELECT "Id" FROM "Companies" WHERE "Slug" = 'slug-da-empresa')
  AND "FeatureKey" = 'own_delivery';
```

**Consultar todas as flags de um tenant:**
```sql
SELECT f."FeatureKey", f."IsEnabled", f."UpdatedAtUtc"
FROM "CompanyFeatureOverrides" f
JOIN "Companies" c ON f."CompanyId" = c."Id"
WHERE c."Slug" = 'slug-da-empresa'
ORDER BY f."FeatureKey";
```

### 4.3 Como as flags chegam ao frontend

O endpoint `GET /public/tenant/resolve?slug={slug}` retorna o objeto `features` resolvido:

```json
{
  "slug": "suaempresa",
  "plan": "trial",
  "features": {
    "agenda": false,
    "commissions": true,
    "own_delivery": true,
    "financial_menu": true,
    "loyalty_program": true,
    "accounting_email_dispatch": false
  }
}
```

O frontend usa `canAccess(module, role, features)` em `src/config/modules.ts` para filtrar os módulos exibidos no painel. Módulos com `featureKey` ausente no mapa de features assumem `true` por padrão (fail-open).

---

## 5. Domínio Próprio

Todo tenant nasce com subdomínio padrão `slug.vendapps.com.br`. Tenants em planos superiores podem configurar um domínio próprio.

### 5.1 Fluxo de configuração

1. **Tenant solicita domínio** → painel da loja em `/app/configuracao-loja` → seção "Domínio próprio"
2. **Sistema cria registro** em `CompanyCustomDomains` com `Status = "pending"` e gera `VerificationToken`
3. **Tenant configura DNS** — deve adicionar registro CNAME:
   ```
   pedidos.sualoja.com.br  CNAME  slug.vendapps.com.br
   ```
   e registro TXT de verificação:
   ```
   _vendapps-verify.pedidos.sualoja.com.br  TXT  "<VerificationToken>"
   ```
4. **Verificação** → endpoint valida TXT → atualiza `Status = "verified"`, preenche `VerifiedAtUtc`
5. **Ativação** → após verificação: `Status = "active"` → CORS e resolução habilitados

### 5.2 Status do domínio

| Status | Significado |
|---|---|
| `pending` | Aguardando configuração de DNS pelo tenant |
| `verified` | DNS verificado, aguardando propagação ou ativação manual |
| `active` | Ativo — CORS liberado, tenant resolve por este hostname |
| `disabled` | Desabilitado manualmente |

### 5.3 CORS dinâmico para domínio próprio

O backend consulta `CompanyCustomDomains` em cada requisição de origem desconhecida. O resultado é cacheado em memória:
- Domínio **permitido**: cache de 10 minutos
- Domínio **não encontrado**: cache de 1 minuto

**Ativar domínio manualmente:**
```sql
UPDATE "CompanyCustomDomains"
SET "Status" = 'active', "VerifiedAtUtc" = now(), "UpdatedAtUtc" = now()
WHERE "Hostname" = 'pedidos.sualoja.com.br';
```

**Desabilitar domínio:**
```sql
UPDATE "CompanyCustomDomains"
SET "Status" = 'disabled', "UpdatedAtUtc" = now()
WHERE "Hostname" = 'pedidos.sualoja.com.br';
```

### 5.4 Configuração no Vercel (frontend)

O frontend é uma SPA hospedada na Vercel. Para que um domínio próprio funcione:

1. Adicionar o hostname no projeto Vercel (Settings → Domains)
2. Configurar o CNAME do domínio apontando para o Vercel
3. Garantir que `vercel.json` tem o rewrite SPA configurado:
   ```json
   {
     "rewrites": [{ "source": "/((?!api/).*)", "destination": "/index.html" }]
   }
   ```
4. A variável `VITE_API_URL` já aponta para a API centralizada — não precisa alterar por tenant

---

## 6. Branding

Cada tenant tem uma entrada em `StoreFrontConfigs` (relação 1:1 com `Companies`). Se não existir, o sistema usa defaults.

### 6.1 Campos configuráveis

| Campo | Tipo | Padrão | Descrição |
|---|---|---|---|
| `StoreName` | `varchar(120)` | Nome da empresa | Nome exibido no header da loja |
| `StoreSlogan` | `varchar(200)` | `null` | Subtítulo/slogan abaixo do nome |
| `LogoUrl` | `text` | `null` | URL HTTPS ou data URI base64 da logo |
| `PrimaryColor` | `varchar(20)` | `#6366f1` | Cor principal (botões, badges, destaques) |
| `SecondaryColor` | `varchar(20)` | `#1E3A8A` | Cor secundária (navy) |
| `AccentColor` | `varchar(20)` | `#f59e0b` | Cor de acento (dourado) |
| `TextColor` | `varchar(20)` | `#111827` | Cor do texto principal |
| `TextMutedColor` | `varchar(20)` | `#6b7280` | Cor do texto secundário |
| `BgColor` | `varchar(50)` | `#ffffff` | Cor de fundo da página |
| `Surface2Color` | `varchar(50)` | `#f3f4f6` | Cor de cards e superfícies secundárias |
| `BorderColor` | `varchar(50)` | `rgba(0,0,0,0.08)` | Cor das bordas |
| `CatalogStyle` | `varchar(30)` | `default` | Layout do catálogo: `default` ou `petshop` |
| `BannerIntervalSecs` | `int` | `5` | Intervalo de slides em segundos (0 = manual) |
| `AnnouncementsJson` | `text` | `["Frete Grátis..."]` | Array JSON de mensagens no banner de anúncios |

### 6.2 Criar branding padrão para novo tenant

```sql
INSERT INTO "StoreFrontConfigs" (
  "Id", "CompanyId",
  "PrimaryColor", "BgColor", "Surface2Color", "BorderColor",
  "TextColor", "TextMutedColor", "SecondaryColor", "AccentColor",
  "CatalogStyle", "BannerIntervalSecs", "AnnouncementsJson"
) VALUES (
  gen_random_uuid(),
  (SELECT "Id" FROM "Companies" WHERE "Slug" = 'slug-da-empresa'),
  '#7c5cf8',               -- brand padrão vendApps
  '#ffffff',
  '#f3f4f6',
  'rgba(0,0,0,0.08)',
  '#111827',
  '#6b7280',
  '#1E3A8A',
  '#f59e0b',
  'default',
  5,
  '["Bem-vindo à nossa loja!"]'
);
```

### 6.3 Atualizar cor primária de um tenant

```sql
UPDATE "StoreFrontConfigs"
SET "PrimaryColor" = '#e85d04', "UpdatedAtUtc" = now()
WHERE "CompanyId" = (SELECT "Id" FROM "Companies" WHERE "Slug" = 'slug-da-empresa');
```

### 6.4 Logo

A logo aceita dois formatos:
- **URL HTTPS**: `"https://cdn.sualoja.com.br/logo.png"`
- **Data URI base64**: `"data:image/png;base64,iVBORw0KGgo..."` (máx. recomendado: 200 KB)

O backend decodifica data URIs para embedding em PDFs e emails (`TryDecodeDataUriLogo()`).

### 6.5 CatalogStyle

| Valor | Comportamento |
|---|---|
| `default` | Header branco, layout neutro |
| `petshop` | Header escuro com ícones de patas, paleta animal |

---

## 7. Variáveis de Ambiente

### Backend (Render)

| Variável | Obrigatória | Descrição |
|---|:---:|---|
| `ConnectionStrings__Default` | ✅ | Connection string PostgreSQL |
| `Jwt__Key` | ✅ | Secret key para assinar tokens JWT (mín. 32 chars) |
| `Jwt__Issuer` | ✅ | Issuer do JWT — padrão: `"vendapps"` |
| `Jwt__Audience` | ✅ | Audience do JWT — padrão: `"vendapps"` |
| `Jwt__AdminUser` | ✅ | Username do admin master (seeder) |
| `Jwt__AdminPassword` | ✅ | Senha do admin master (seeder) |
| `Jwt__CompanyId` | ✅ | CompanyId do tenant padrão para o admin master |
| `TENANT_BASE_DOMAIN` | ❌ | Domínio base para resolução de slug. Padrão: `vendapps.com.br` |
| `ALLOWED_ORIGINS` | ❌ | Lista de origens CORS extras, separadas por vírgula |
| `Master__Enabled` | ❌ | `"true"` para habilitar rotas `/master/*`. Padrão: `false` |
| `ENABLE_SWAGGER` | ❌ | `"true"` para habilitar Swagger UI. Padrão: `false` |
| `PORT` | ❌ | Porta HTTP. Padrão: `5082` (Render define automaticamente) |

### Frontend (Vercel)

| Variável | Obrigatória | Descrição |
|---|:---:|---|
| `VITE_API_URL` | ✅ | URL da API backend — `https://vendapps.onrender.com` |
| `VITE_TENANT_BASE_DOMAIN` | ❌ | Domínio base para extração de slug. Padrão: `vendapps.com.br` |

---

## 8. Fluxo de Resolução de Tenant

```
Usuário acessa: suaempresa.vendapps.com.br
                        │
          ┌─────────────▼──────────────┐
          │ Frontend: resolveTenantFromHost()   │
          │ extrai slug = "suaempresa"  │
          └─────────────┬──────────────┘
                        │
          GET /public/tenant/resolve?slug=suaempresa
                        │
          ┌─────────────▼──────────────┐
          │ PublicTenantController     │
          │ 1. Busca Companies.Slug    │
          │ 2. Valida IsActive, !Deleted│
          │ 3. Verifica SuspendedAtUtc │
          │ 4. ResolveFeaturesAsync()  │
          └─────────────┬──────────────┘
                        │
          ┌─────────────▼──────────────┐
          │ PlanFeatureService         │
          │ 1. Defaults do plan=trial  │
          │ 2. + CompanyFeatureOverrides│
          │ 3. Cache 5 min por company │
          └─────────────┬──────────────┘
                        │
          Response: { slug, plan, features, ... }
                        │
          ┌─────────────▼──────────────┐
          │ Frontend usa features para │
          │ filtrar módulos visíveis   │
          │ canAccess(module, role, f) │
          └─────────────────────────── ┘

Fluxo de autenticação admin:
POST /auth/login → JWT com { companyId, role, name }
                                │
          ┌─────────────────────▼──────────────────────┐
          │ TenantHostValidationMiddleware              │
          │ Compara JWT.companyId == Host.companyId     │
          │ Se diverge → 403 "Token inválido"           │
          └─────────────────────────────────────────────┘
```

---

## 9. Operações SQL Comuns

### Listar todos os tenants ativos

```sql
SELECT "Slug", "Name", "Segment", "Plan", "IsActive", "SuspendedAtUtc", "CreatedAtUtc"
FROM "Companies"
WHERE "IsDeleted" = false
ORDER BY "CreatedAtUtc" DESC;
```

### Suspender um tenant

```sql
UPDATE "Companies"
SET "SuspendedAtUtc" = now(),
    "SuspendedReason" = 'Inadimplência — fatura #123 em aberto'
WHERE "Slug" = 'slug-da-empresa';
```

### Reativar tenant suspenso

```sql
UPDATE "Companies"
SET "SuspendedAtUtc" = null,
    "SuspendedReason" = null
WHERE "Slug" = 'slug-da-empresa';
```

### Ver features efetivas de um tenant

```sql
SELECT
  c."Slug",
  c."Plan",
  o."FeatureKey",
  o."IsEnabled" AS "Override",
  o."UpdatedAtUtc"
FROM "Companies" c
LEFT JOIN "CompanyFeatureOverrides" o ON o."CompanyId" = c."Id"
WHERE c."Slug" = 'slug-da-empresa'
ORDER BY o."FeatureKey";
```

### Ver domínios customizados de um tenant

```sql
SELECT d."Hostname", d."Status", d."VerifiedAtUtc", d."CreatedAtUtc"
FROM "CompanyCustomDomains" d
JOIN "Companies" c ON d."CompanyId" = c."Id"
WHERE c."Slug" = 'slug-da-empresa';
```

### Ver branding atual de um tenant

```sql
SELECT
  c."Name", c."Slug",
  s."StoreName", s."PrimaryColor", s."CatalogStyle",
  s."LogoUrl" IS NOT NULL AS "TemLogo",
  s."UpdatedAtUtc"
FROM "Companies" c
JOIN "StoreFrontConfigs" s ON s."CompanyId" = c."Id"
WHERE c."Slug" = 'slug-da-empresa';
```

### Contar tenants por plano

```sql
SELECT "Plan", COUNT(*) AS total
FROM "Companies"
WHERE "IsDeleted" = false AND "IsActive" = true
GROUP BY "Plan"
ORDER BY total DESC;
```

---

## 10. Troubleshooting

### Tenant retorna 404 no `/public/tenant/resolve`

1. Verificar se `Companies.Slug` corresponde exatamente ao subdomínio (lowercase, sem espaços)
2. Verificar se `IsActive = true` e `IsDeleted = false`
3. Verificar se `SuspendedAtUtc` está preenchido (retorna 403, não 404)

### CORS bloqueando requisição de domínio próprio

1. Verificar `CompanyCustomDomains.Status` — deve ser `"active"` ou `"verified"`
2. O cache de CORS é de 10 min — aguardar ou reiniciar API
3. Verificar se `Hostname` está exatamente igual ao `Origin` da requisição (sem `https://`)

### Feature não aparece no frontend após override

O cache de features expira em 5 minutos. Para forçar, reinicie a API ou espere o intervalo. O frontend também faz cache local da `TenantInfo` via React Query com `staleTime: 5 * 60 * 1000`.

### Admin retorna 403 com "Token inválido para este domínio"

O JWT foi emitido para um `companyId` diferente do subdomínio acessado. O usuário precisa fazer login no subdomínio correto. Não é um bug — é a proteção de isolamento entre tenants.

### Branding não atualiza após mudança no banco

O frontend faz cache do branding via React Query. O tenant precisa recarregar a página ou o cache expira naturalmente após 5 minutos.

### Seeder não cria tenant de demo

O seeder é idempotente por `Slug`. Se o tenant já existe com aquele slug, nenhuma alteração é feita. Para resetar um tenant de demo: delete a empresa pelo slug e reinicie a API.

```sql
-- CUIDADO: delete em cascata apaga todos os dados do tenant
DELETE FROM "Companies" WHERE "Slug" = 'petshop-demo';
-- Reiniciar API para o seeder recriar
```
