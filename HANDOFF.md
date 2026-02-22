# vendApps — Handoff Completo do Projeto

> Documento para onboarding de IA ou desenvolvedor. Cobre toda a base de código atual.

---

## 1. Visão Geral

**vendApps** é uma plataforma fullstack de vendas e delivery multi-empresa. Permite que empresas (ex: petshops) tenham um catálogo online, recebam pedidos via WhatsApp, gerenciem estoque/produtos, planejem rotas de entrega e acompanhem tudo por um painel admin.

**Repositório:** `https://github.com/mkinfoservice/vendApps`
**Branch principal:** `main`
**Pasta local:** `d:\DEV\petshop` (em processo de renomear para `d:\DEV\vendApps`)

---

## 2. Stack Técnica

| Camada | Tecnologia |
|---|---|
| Backend | ASP.NET Core .NET 8, EF Core 8, PostgreSQL |
| Frontend | React 18, Vite, TypeScript, Tailwind CSS |
| HTTP Client (admin) | `adminFetch` (wrapper sobre `fetch` com JWT) |
| State/Cache | TanStack Query (React Query) v5 |
| Auth | JWT — roles: `admin`, `deliverer` |
| Jobs / Scheduling | Hangfire 1.8 + PostgreSQL storage + Cronos 0.8 |
| Sync de Produtos | CSV (CsvHelper 33), REST API, DB (stub) |
| Imagens | `LocalImageStorageProvider` → `wwwroot/product-images/` |
| Geocoding | OpenRouteService (ORS) — rotas de entrega |
| Roteamento Frontend | React Router v6 |
| Ícones | Lucide React |

---

## 3. Estrutura do Projeto

```
vendApps/
├── backend/
│   └── Petshop.Api/                    ← monolito ASP.NET Core
│       ├── Controllers/
│       │   ├── AuthController.cs        POST /auth/login, /auth/deliverer/login
│       │   ├── CatalogController.cs     GET /catalog/{slug}/products|categories
│       │   ├── OrdersController.cs      CRUD pedidos (público + admin)
│       │   ├── DashboardController.cs   GET /dashboard/summary
│       │   ├── FinanceiroController.cs  GET /financeiro/...
│       │   ├── RoutesController.cs      CRUD rotas de entrega
│       │   ├── DeliverersController.cs  CRUD entregadores
│       │   ├── DelivererPortalController.cs  App do entregador
│       │   ├── AdminProductsController.cs    CRUD produtos admin
│       │   ├── AdminProductSourcesController.cs  Fontes de sync
│       │   └── AdminProductSyncController.cs     Jobs de sync
│       ├── Entities/
│       │   ├── Order.cs, OrderItem.cs, OrderStatus.cs, PaymentMethod.cs
│       │   ├── Delivery/
│       │   │   ├── Deliverer.cs         entregador (phone, pinHash, vehicle)
│       │   │   ├── Route.cs             rota com paradas
│       │   │   ├── RouteStop.cs         parada = pedido em uma rota
│       │   │   ├── RouteStatus.cs       enum: Draft/Active/Completed/Cancelled
│       │   │   └── RouteStopStatus.cs   enum: Pending/Delivered/Attempted/Problem
│       │   ├── Catalog/
│       │   │   ├── Company.cs           tenant raiz (Id, Name, Slug, SettingsJson)
│       │   │   ├── Brand.cs             marca por empresa
│       │   │   ├── ProductVariant.cs    variações (cor, tamanho…)
│       │   │   └── ProductImage.cs      múltiplas imagens por produto
│       │   ├── Sync/
│       │   │   ├── ExternalSource.cs    fonte externa (CSV/REST/DB)
│       │   │   ├── ExternalProductSnapshot.cs  último hash sincronizado
│       │   │   ├── ProductSyncJob.cs    job de sync com stats
│       │   │   └── ProductSyncItem.cs   item individual do job
│       │   └── Audit/
│       │       ├── ProductChangeLog.cs  log de campo alterado
│       │       └── ProductPriceHistory.cs  histórico de preços
│       ├── Models/
│       │   ├── Products.cs              Product (expandido: CompanyId, BrandId,
│       │   │                            InternalCode, Barcode, CostCents,
│       │   │                            MarginPercent, StockQty, Ncm, RowVersion…)
│       │   └── Category.cs             Category (CompanyId adicionado)
│       ├── Data/
│       │   ├── AppDbContext.cs          18+ DbSets, índices compostos
│       │   └── DbSeeder.cs             seed automático (Company + catálogo dev)
│       ├── Services/
│       │   ├── Sync/
│       │   │   ├── IProductProvider.cs
│       │   │   ├── ConnectorFactory.cs
│       │   │   ├── ProductSyncService.cs
│       │   │   ├── SyncSchedulerJob.cs  (Hangfire, roda a cada minuto)
│       │   │   ├── ProductHashService.cs  (SHA-256)
│       │   │   ├── SyncMergePolicyService.cs
│       │   │   └── Connectors/
│       │   │       ├── CsvProductProvider.cs
│       │   │       ├── RestApiProductProvider.cs
│       │   │       └── DbProductProvider.cs  (stub)
│       │   └── Images/
│       │       ├── IImageStorageProvider.cs
│       │       └── LocalImageStorageProvider.cs
│       ├── Contracts/Admin/
│       │   ├── Products/   CreateProductRequest, UpdateProductRequest,
│       │   │               ProductListResponse, ProductDetailResponse
│       │   ├── Sources/    CreateSourceRequest, UpdateSourceRequest, SourceResponse
│       │   └── Sync/       SyncContracts.cs
│       ├── Migrations/
│       │   └── 20260220212026_InitialProductModule  ← migração única (drop+recreate)
│       └── wwwroot/
│           └── product-images/   ← imagens salvas localmente (no .gitignore)
│
└── frontend/
    └── petshop-web/               ← React + Vite
        ├── .env.local             VITE_API_URL + VITE_COMPANY_SLUG
        └── src/
            ├── App.tsx            catálogo público (/)
            ├── routes.tsx         todas as rotas da SPA
            ├── main.tsx           QueryClientProvider + CartProvider
            ├── lib/api.ts         axios instance (não usada no admin, só pública)
            ├── features/
            │   ├── admin/
            │   │   ├── auth/
            │   │   │   ├── auth.ts          getToken / setToken / clearToken
            │   │   │   ├── adminFetch.ts    fetch autenticado (injeta JWT, trata 401)
            │   │   │   ├── api.ts           login()
            │   │   │   └── Guard.tsx        <AdminGuard>
            │   │   ├── products/
            │   │   │   ├── api.ts           fetchAdminProducts, create, update,
            │   │   │   │                    toggle, delete, uploadImage, deleteImage
            │   │   │   └── queries.ts       hooks React Query
            │   │   ├── orders/
            │   │   │   ├── api.ts           fetchOrders, fetchOrderById, updateStatus
            │   │   │   ├── queries.ts
            │   │   │   ├── status.ts        enum OrderStatus
            │   │   │   └── payment.ts       paymentLabel()
            │   │   ├── routes/
            │   │   │   ├── api.ts
            │   │   │   ├── plannerApi.ts
            │   │   │   ├── queries.ts
            │   │   │   ├── status.ts
            │   │   │   └── types.ts
            │   │   ├── dashboard/
            │   │   │   ├── api.ts
            │   │   │   └── queries.ts
            │   │   └── financeiro/
            │   │       ├── api.ts
            │   │       └── queries.ts
            │   ├── catalog/
            │   │   ├── api.ts        fetchCategories, fetchProducts (usa VITE_COMPANY_SLUG)
            │   │   ├── queries.ts    useCategories, useProducts
            │   │   ├── CategoryTile.tsx
            │   │   ├── ProductCard.tsx
            │   │   └── ProductRow.tsx
            │   ├── cart/
            │   │   ├── cart.tsx      CartContext (persiste em localStorage)
            │   │   └── CartSheet.tsx
            │   └── deliverer/
            │       ├── auth/Guard.tsx
            │       └── components/   NextStopCard, StopListItem, NavigationButtons…
            ├── pages/
            │   ├── Checkout.tsx           fluxo de pedido + ViaCEP + WhatsApp
            │   ├── admin/
            │   │   ├── Login.tsx
            │   │   ├── Dashboard.tsx
            │   │   ├── OrdersList.tsx     tabela paginada + filtros
            │   │   ├── OrderDetail.tsx    detalhe + troca de status
            │   │   ├── ProductsList.tsx   tabela + toggle inline + delete
            │   │   ├── ProductForm.tsx    criar/editar + upload de imagens
            │   │   ├── RoutesList.tsx
            │   │   ├── RoutePlanner.tsx
            │   │   ├── RouteDetail.tsx
            │   │   └── Financeiro.tsx
            │   └── deliverer/
            │       ├── Login.tsx   (phone + PIN)
            │       ├── Home.tsx
            │       └── RouteDetail.tsx
            └── components/
                ├── admin/AdminNav.tsx   nav com: Dashboard, Pedidos, Produtos, Rotas, Financeiro
                ├── HeroMarket.tsx
                ├── TopBar.tsx
                └── ui/   ThemeToggle, Button, Badge, Input, Sheet (shadcn-style)
```

---

## 4. Multiempresa (Multitenancy)

Toda entidade de catálogo possui `CompanyId` (Guid). O catálogo público usa slug da empresa na URL:

```
GET /catalog/{companySlug}/products?categorySlug=racao&search=premium
GET /catalog/{companySlug}/categories
```

O JWT do admin carrega o claim `companyId`. Todos os controllers admin fazem:
```csharp
private Guid CompanyId => Guid.Parse(User.FindFirstValue("companyId")!);
```

**Dev defaults (seed automático):**
- Company GUID: `11111111-0000-0000-0000-000000000001`
- Company slug: `petshop-demo`
- Admin user: `admin` / senha: `admin123`
- JWT configurado em `appsettings.Development.json`

---

## 5. Banco de Dados

**Migração única:** `20260220212026_InitialProductModule`
(antigas migrações foram deletadas, banco foi dropado e recriado)

**Principais tabelas:**

| Tabela | Chave única / índice notable |
|---|---|
| `Companies` | `Slug` único |
| `Products` | `(CompanyId, Slug)` único; `RowVersion` concurrency token |
| `Categories` | `(CompanyId, Slug)` único |
| `Brands` | `(CompanyId, Slug)` único |
| `ProductImages` | FK para Product |
| `ProductVariants` | FK para Product |
| `ExternalProductSnapshots` | `(CompanyId, ExternalSourceId, ExternalId)` único |
| `ProductSyncJobs` | FK para Company + ExternalSource |
| `ProductSyncItems` | FK para Job |
| `ProductChangeLogs` | FK para Company + Product |
| `ProductPriceHistories` | FK para Product |
| `Orders` | `PublicId` único |
| `OrderItems` | FK para Order + Product |
| `Routes` | FK para Company |
| `RouteStops` | FK para Route + Order |
| `Deliverers` | `Phone` único; `PinHash` (BCrypt) |

**Seeder:** roda automaticamente na inicialização se o banco estiver vazio.
Para recriar do zero: delete o banco e rode `dotnet run`.

---

## 6. Autenticação

### Admin
- `POST /auth/login` com `{ username, password }`
- Retorna JWT com claims: `sub`, `name`, `role=admin`, `companyId`, `jti`
- Expiração: 8 horas

### Entregador
- `POST /auth/deliverer/login` com `{ phone, pin }`
- PIN comparado via BCrypt (`deliverer.PinHash`)
- Retorna JWT com claims: `sub`, `name`, `role=deliverer`, `delivererId`

### Frontend
- Token salvo/lido via `auth.ts` (`getToken` / `setToken` / `clearToken`)
- `adminFetch.ts` injeta `Authorization: Bearer <token>` em todas as chamadas admin
- Em `401`: limpa token e redireciona para `/admin/login`
- `<AdminGuard>` e `<DelivererGuard>` protegem rotas no React Router

---

## 7. API — Endpoints Completos

### Público
```
POST   /auth/login                     login admin
POST   /auth/deliverer/login           login entregador
POST   /orders                         criar pedido (cliente)
GET    /catalog/{slug}/products        listar produtos por empresa
GET    /catalog/{slug}/categories      listar categorias por empresa
```

### Admin — Pedidos `[Authorize(Roles="admin")]`
```
GET    /orders                         listar (paginado, filtro status/search)
GET    /orders/{idOrNumber}            detalhe
PATCH  /orders/{idOrNumber}/status     alterar status
```

### Admin — Produtos `[Authorize(Roles="admin")]`
```
GET    /admin/products                 listar (page, pageSize, search, categoryId, active)
POST   /admin/products                 criar
GET    /admin/products/{id}            detalhe (com images + variants)
PUT    /admin/products/{id}            editar
PATCH  /admin/products/{id}/toggle-status  ativar/desativar
DELETE /admin/products/{id}            soft delete (isActive=false)
POST   /admin/products/{id}/clone      clonar produto
POST   /admin/products/{id}/images     upload imagem (multipart, max 10MB)
DELETE /admin/products/{id}/images/{imageId}  excluir imagem
GET    /admin/products/{id}/price-history     histórico de preços (últimos 50)
GET    /admin/products/{id}/changelogs        log de alterações (últimos 100)
```

### Admin — Fontes de Sync `[Authorize(Roles="admin")]`
```
GET    /admin/product-sources          listar
POST   /admin/product-sources          criar
PUT    /admin/product-sources/{id}     editar
DELETE /admin/product-sources/{id}     excluir
POST   /admin/product-sources/{id}/test  testar conexão
```

### Admin — Jobs de Sync `[Authorize(Roles="admin")]`
```
POST   /admin/products/sync            disparar sync manual
GET    /admin/products/sync/jobs       listar jobs
GET    /admin/products/sync/jobs/{id}  detalhe do job
GET    /admin/products/sync/jobs/{id}/items  itens do job
POST   /admin/products/sync/jobs/{id}/retry  retentar job
```

### Admin — Rotas / Entregadores
```
GET    /routes                         listar rotas
POST   /routes                         criar rota
GET    /routes/{id}                    detalhe
PUT    /routes/{id}                    editar
DELETE /routes/{id}                    excluir
POST   /routes/{id}/start              iniciar rota
POST   /routes/{id}/complete           concluir rota
GET    /deliverers                     listar entregadores
POST   /deliverers                     criar entregador
PUT    /deliverers/{id}                editar
DELETE /deliverers/{id}                excluir
```

### Entregador `[Authorize(Roles="deliverer")]`
```
GET    /deliverer/route                rota ativa do entregador
PATCH  /deliverer/stops/{stopId}       atualizar status da parada
```

### Dashboard / Financeiro
```
GET    /dashboard/summary              métricas operacionais
GET    /financeiro/...                 relatórios financeiros
```

---

## 8. Módulo de Sync de Produtos

### Fluxo
1. `SyncSchedulerJob` roda a cada minuto (Hangfire recurring)
2. Busca `ExternalSources` com `SyncMode=Scheduled` e cron vencido
3. Para cada fonte, chama `ProductSyncService.RunAsync(sourceId)`
4. O serviço: cria Job → busca páginas do conector → computa SHA-256 → compara snapshot → aplica merge policy → upsert Product → registra auditoria → atualiza snapshot → finaliza Job com stats

### Conectores disponíveis
| Tipo | Classe | Status |
|---|---|---|
| CSV | `CsvProductProvider` | Funcional |
| REST API | `RestApiProductProvider` | Funcional |
| DB | `DbProductProvider` | Stub (NotImplementedException) |

### Merge Policy
Lida de `Company.SettingsJson`. Default: `PreferExternal` (atualiza todos os campos do produto com dados externos).

### Auditoria
- `ProductChangeLog`: registra cada campo alterado (quem, quando, de→para)
- `ProductPriceHistory`: snapshot de preço/custo/margem a cada alteração

---

## 9. Frontend — Padrões de Código

### Chamadas API (área admin)
```typescript
// Sempre via adminFetch (injeta JWT, trata 401)
import { adminFetch } from "@/features/admin/auth/adminFetch";

export async function fetchAdminProducts(params) {
  return adminFetch<ProductListResponse>(`/admin/products?${qs}`);
}
```

### Hooks React Query
```typescript
export function useAdminProducts(params) {
  return useQuery({
    queryKey: ["admin-products", params],
    queryFn: () => fetchAdminProducts(params),
  });
}

export function useCreateProduct() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data) => createAdminProduct(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["admin-products"] }),
  });
}
```

### Catálogo público
```typescript
// Usa VITE_COMPANY_SLUG e VITE_API_URL
const COMPANY_SLUG = import.meta.env.VITE_COMPANY_SLUG ?? "petshop-demo";
await fetch(`${API_URL}/catalog/${COMPANY_SLUG}/products?categorySlug=racao`);
```

### Env vars do frontend (`.env.local`)
```
VITE_API_URL=http://localhost:5082
VITE_COMPANY_SLUG=petshop-demo
```

### Tema
CSS variables no `:root`: `--bg`, `--surface`, `--surface-2`, `--border`, `--text`, `--text-muted`.
Cor da marca: `#7c5cf8` (purple). Suporte a dark/light mode via `ThemeToggle`.

---

## 10. Como Rodar Localmente

### Pré-requisitos
- .NET 8 SDK
- Node.js 18+
- PostgreSQL 14+

### Backend
```bash
cd backend/Petshop.Api
dotnet restore
dotnet run
# API: http://localhost:5082
# Hangfire: http://localhost:5082/admin/hangfire
```

### Frontend
```bash
cd frontend/petshop-web
npm install
npm run dev
# http://localhost:5173
```

### Banco
```bash
docker-compose up -d   # ou configure PostgreSQL manualmente
```
Conexão dev: `Host=localhost;Port=5432;Database=petshop_db;Username=petshop;Password=petshop`

---

## 11. Estado Atual — O que está pronto

| Módulo | Estado |
|---|---|
| Catálogo público (cliente) | Completo |
| Carrinho + Checkout + WhatsApp | Completo |
| Auth admin (JWT) | Completo |
| Painel Admin — Dashboard | Completo |
| Painel Admin — Pedidos | Completo |
| Painel Admin — Produtos (CRUD + imagens) | Completo |
| Painel Admin — Rotas de entrega | Completo |
| Painel Admin — Financeiro | Completo |
| App Entregador (mobile-first) | Completo |
| Multiempresa (CompanyId em tudo) | Completo |
| Sync de produtos (CSV/REST) | Completo |
| Sync DB conector | Stub (não implementado) |
| Upload imagem local | Completo |
| Upload imagem S3/R2 | Interface pronta, não implementado |
| Geocoding de endereços (ORS) | Configurado |

---

## 12. Decisões Técnicas Importantes

- **Migração consolidada:** todas as migrações antigas foram deletadas e recriadas como uma única (`InitialProductModule`). O banco precisa ser dropado para aplicar do zero.
- **Slug único por empresa:** índice composto `(CompanyId, Slug)` em Products, Categories e Brands — não é global.
- **Soft delete em produtos:** `DELETE /admin/products/{id}` apenas seta `IsActive = false`.
- **Token JWT precisa ter `companyId`:** tokens gerados antes da implementação do módulo de produtos não têm esse claim e causam 500 nos endpoints admin de produtos. Solução: fazer logout e login novamente.
- **`wwwroot/product-images/`** precisa existir para upload funcionar. O diretório está no `.gitignore` com `.gitkeep` rastreado.
- **Hangfire dashboard** disponível apenas em desenvolvimento em `/admin/hangfire`.
- **RowVersion** no Product para controle de concorrência otimista (EF Core `[Timestamp]`).

---

## 13. Commits Principais

| Hash | Descrição |
|---|---|
| `b147112` | Commit inicial |
| `9a88d8c` | Redesign frontend + Dashboard + Financeiro |
| `62868ed` | Módulo de produtos completo (backend) |
| `d60f134` | Cadastro de produtos no admin (frontend) |
| `ee732dd` | README reescrito + limpeza de docs |
