import { lazy, Suspense, type ReactNode } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import App from "./App";
import { AdminGuard } from "@/features/admin/auth/Guard";
import { MasterGuard } from "@/features/master/auth/Guard";
import { DelivererGuard } from "@/features/deliverer/auth/Guard";
import { AppShell } from "@/app/layout/AppShell";
import { ModuleGuard } from "@/app/components/ModuleGuard";
import { PdvProvider } from "@/features/pdv/PdvContext";

const Checkout = lazy(() => import("./pages/Checkout"));
const ProductDetail = lazy(() => import("./pages/ProductDetail"));
const LoyaltyPage = lazy(() => import("./pages/LoyaltyPage"));
const LoginPage = lazy(() => import("./pages/Login"));
const ForgotPasswordPage = lazy(() => import("./pages/ForgotPassword"));
const ResetPasswordPage = lazy(() => import("./pages/ResetPassword"));
const OperationCenter = lazy(() => import("@/app/pages/OperationCenter"));
const MesaPage = lazy(() => import("./pages/MesaPage"));

const Dashboard = lazy(() => import("./pages/admin/Dashboard"));
const OrdersList = lazy(() => import("./pages/admin/OrdersList"));
const OrderDetail = lazy(() => import("./pages/admin/OrderDetail"));
const ProductsList = lazy(() => import("./pages/admin/ProductsList"));
const ProductForm = lazy(() => import("./pages/admin/ProductForm"));
const DeliverersList = lazy(() => import("./pages/admin/DeliverersList"));
const DelivererForm = lazy(() => import("./pages/admin/DelivererForm"));
const RoutesList = lazy(() => import("./pages/admin/RoutesList"));
const RouteDetail = lazy(() => import("./pages/admin/RouteDetail"));
const RoutePlanner = lazy(() => import("./pages/admin/RoutePlanner"));
const StoreTeam = lazy(() => import("./pages/admin/StoreTeam"));
const PrintQueue = lazy(() => import("./pages/admin/PrintQueue"));
const MobilePrintAgentPage = lazy(() => import("./pages/admin/MobilePrintAgentPage"));
const AtendimentoHub = lazy(() => import("./pages/admin/AtendimentoHub"));
const PhoneOrderBuilder = lazy(() => import("./pages/admin/PhoneOrderBuilder"));
const CustomersList = lazy(() => import("./pages/admin/CustomersList"));
const CustomerDetail = lazy(() => import("./pages/admin/CustomerDetail"));
const CustomerForm = lazy(() => import("./pages/admin/CustomerForm"));
const CustomersPage = lazy(() => import("./pages/admin/CustomersPage"));
const LoyaltyConfigPage = lazy(() => import("./pages/admin/LoyaltyConfigPage"));
const PromotionsPage = lazy(() => import("./pages/admin/PromotionsPage"));
const SuppliersPage = lazy(() => import("./pages/admin/SuppliersPage"));
const SuppliesPage = lazy(() => import("./pages/admin/SuppliesPage"));
const PurchasesPage = lazy(() => import("./pages/admin/PurchasesPage"));
const PurchaseOrderDetail = lazy(() => import("./pages/admin/PurchaseOrderDetail"));
const ReportsPage = lazy(() => import("./pages/admin/ReportsPage"));
const StockPage = lazy(() => import("./pages/admin/StockPage"));
const FiscalConfigPage = lazy(() => import("./pages/admin/FiscalConfigPage"));
const AccountingDispatchPage = lazy(() => import("./pages/admin/AccountingDispatchPage"));
const FinancialEntriesPage = lazy(() => import("./pages/admin/FinancialEntriesPage"));
const CashRegistersPage = lazy(() => import("./pages/admin/CashRegistersPage"));
const CashSessionsPage = lazy(() => import("./pages/admin/CashSessionsPage"));
const AgendaPage = lazy(() => import("./pages/admin/AgendaPage"));
const CommissionsPage = lazy(() => import("./pages/admin/CommissionsPage"));
const ScaleAgentsPage = lazy(() => import("./pages/admin/ScaleAgentsPage"));
const DavListPage = lazy(() => import("./pages/admin/DavListPage"));
const DavBuilderPage = lazy(() => import("./pages/admin/DavBuilderPage"));
const CatalogEnrichmentPage = lazy(() => import("./pages/admin/CatalogEnrichmentPage"));
const PdvSalesPage = lazy(() => import("./pages/admin/PdvSalesPage"));
const FiscalDocumentsPage = lazy(() => import("./pages/admin/FiscalDocumentsPage"));
const DeliveriesPage = lazy(() => import("./pages/admin/DeliveriesPage"));
const StoreFrontConfigPage = lazy(() => import("./pages/admin/StoreFrontConfigPage"));
const OperationalAuditPage = lazy(() => import("./pages/admin/OperationalAuditPage"));
const TablesPage = lazy(() => import("./pages/admin/TablesPage"));
const MarketplacePage = lazy(() => import("./pages/admin/MarketplacePage"));

const PdvPage = lazy(() => import("./pages/pdv/PdvPage"));

const MasterLogin = lazy(() => import("./pages/master/Login"));
const MasterCompanies = lazy(() => import("./pages/master/Companies"));
const MasterCompanyDetail = lazy(() => import("./pages/master/CompanyDetail"));
const MasterPlatformWhatsapp = lazy(() => import("./pages/master/PlatformWhatsapp"));

const DelivererLogin = lazy(() => import("./pages/deliverer/Login"));
const DelivererHome = lazy(() => import("./pages/deliverer/Home"));
const DelivererRouteDetail = lazy(() => import("./pages/deliverer/RouteDetail"));

function RouteFallback() {
  return (
    <div className="min-h-dvh flex items-center justify-center bg-[var(--bg)] text-sm font-semibold text-[var(--text-muted)]">
      Carregando...
    </div>
  );
}

function AppPage({
  children,
  roles,
  featureKey,
}: {
  children: ReactNode;
  roles?: string[];
  featureKey?: string;
}) {
  return (
    <AdminGuard>
      <AppShell>
        <ModuleGuard roles={roles} featureKey={featureKey}>{children}</ModuleGuard>
      </AppShell>
    </AdminGuard>
  );
}

export function AppRoutes() {
  return (
    <BrowserRouter>
      <Suspense fallback={<RouteFallback />}>
        <Routes>
          <Route path="/" element={<App />} />
          <Route path="/mesa/:tableId" element={<MesaPage />} />
          <Route path="/checkout" element={<Checkout />} />
          <Route path="/produto/:id" element={<ProductDetail />} />
          <Route path="/loyalty" element={<LoyaltyPage />} />

          <Route path="/login" element={<LoginPage />} />
          <Route path="/admin/login" element={<Navigate to="/login" replace />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />

          <Route path="/app" element={<AppPage><OperationCenter /></AppPage>} />

          <Route path="/app/mesas" element={<AppPage><TablesPage /></AppPage>} />
          <Route path="/app/pedidos" element={<AppPage><OrdersList /></AppPage>} />
          <Route path="/app/pedidos/:id" element={<AppPage><OrderDetail /></AppPage>} />

          <Route path="/app/atendimento" element={<AppPage><AtendimentoHub /></AppPage>} />
          <Route path="/app/atendimento/pedido" element={<AppPage><PhoneOrderBuilder /></AppPage>} />
          <Route path="/app/atendimento/clientes" element={<AppPage><CustomersList /></AppPage>} />
          <Route path="/app/atendimento/clientes/novo" element={<AppPage><CustomerForm /></AppPage>} />
          <Route path="/app/atendimento/clientes/:id" element={<AppPage><CustomerDetail /></AppPage>} />
          <Route path="/app/atendimento/clientes/:id/editar" element={<AppPage><CustomerForm /></AppPage>} />

          <Route path="/app/dav" element={<AppPage><DavListPage /></AppPage>} />
          <Route path="/app/dav/novo" element={<AppPage><DavBuilderPage /></AppPage>} />

          <Route path="/app/agenda" element={<AppPage featureKey="agenda"><AgendaPage /></AppPage>} />
          <Route path="/app/comissoes" element={<AppPage roles={["admin", "gerente"]} featureKey="commissions"><CommissionsPage /></AppPage>} />
          <Route path="/app/impressao" element={<AppPage><PrintQueue /></AppPage>} />
          <Route path="/app/impressao/mobile" element={<AppPage><MobilePrintAgentPage /></AppPage>} />

          <Route path="/app/produtos" element={<AppPage><ProductsList /></AppPage>} />
          <Route path="/app/produtos/:id" element={<AppPage><ProductForm /></AppPage>} />

          <Route path="/app/clientes" element={<AppPage><CustomersPage /></AppPage>} />
          <Route path="/app/fidelidade" element={<AppPage roles={["admin", "gerente"]} featureKey="loyalty_program"><LoyaltyConfigPage /></AppPage>} />
          <Route path="/app/promocoes" element={<AppPage roles={["admin", "gerente"]}><PromotionsPage /></AppPage>} />

          <Route path="/app/logistica/rotas" element={<AppPage featureKey="own_delivery"><RoutesList /></AppPage>} />
          <Route path="/app/logistica/entregas" element={<AppPage><DeliveriesPage /></AppPage>} />
          <Route path="/app/logistica/rotas/planner" element={<AppPage featureKey="own_delivery"><RoutePlanner /></AppPage>} />
          <Route path="/app/logistica/rotas/:routeId" element={<AppPage featureKey="own_delivery"><RouteDetail /></AppPage>} />
          <Route path="/app/logistica/entregadores" element={<AppPage roles={["admin", "gerente"]} featureKey="own_delivery"><DeliverersList /></AppPage>} />
          <Route path="/app/logistica/entregadores/novo" element={<AppPage roles={["admin", "gerente"]} featureKey="own_delivery"><DelivererForm /></AppPage>} />
          <Route path="/app/logistica/entregadores/:id" element={<AppPage roles={["admin", "gerente"]} featureKey="own_delivery"><DelivererForm /></AppPage>} />

          <Route path="/app/compras" element={<AppPage roles={["admin", "gerente"]}><PurchasesPage /></AppPage>} />
          <Route path="/app/compras/:id" element={<AppPage roles={["admin", "gerente"]}><PurchaseOrderDetail /></AppPage>} />
          <Route path="/app/fornecedores" element={<AppPage roles={["admin", "gerente"]}><SuppliersPage /></AppPage>} />
          <Route path="/app/insumos" element={<AppPage roles={["admin", "gerente"]}><SuppliesPage /></AppPage>} />

          <Route path="/app/financeiro" element={<Navigate to="/app/relatorios" replace />} />
          <Route path="/app/financeiro/lancamentos" element={<AppPage roles={["admin", "gerente"]} featureKey="financial_menu"><FinancialEntriesPage /></AppPage>} />

          <Route path="/app/caixa" element={<AppPage><CashRegistersPage /></AppPage>} />
          <Route path="/app/caixa/sessoes" element={<AppPage><CashSessionsPage /></AppPage>} />
          <Route path="/app/caixa/vendas" element={<AppPage><PdvSalesPage /></AppPage>} />

          <Route path="/app/estoque" element={<AppPage roles={["admin", "gerente"]}><StockPage /></AppPage>} />
          <Route path="/app/relatorios" element={<AppPage roles={["admin", "gerente"]}><ReportsPage /></AppPage>} />

          <Route path="/app/equipe" element={<AppPage roles={["admin"]}><StoreTeam /></AppPage>} />
          <Route path="/app/fiscal" element={<AppPage roles={["admin"]}><FiscalConfigPage /></AppPage>} />
          <Route path="/app/fiscal/documentos" element={<AppPage roles={["admin", "gerente"]}><FiscalDocumentsPage /></AppPage>} />
          <Route path="/app/configuracoes/contabilidade" element={<AppPage roles={["admin", "gerente"]} featureKey="accounting_email_dispatch"><AccountingDispatchPage /></AppPage>} />
          <Route path="/app/balanca" element={<AppPage roles={["admin"]}><ScaleAgentsPage /></AppPage>} />
          <Route path="/app/enriquecimento" element={<AppPage roles={["admin", "gerente"]}><CatalogEnrichmentPage /></AppPage>} />
          <Route path="/app/configuracao-loja" element={<AppPage roles={["admin", "gerente"]}><StoreFrontConfigPage /></AppPage>} />
          <Route path="/app/auditoria" element={<AppPage roles={["admin", "gerente"]}><OperationalAuditPage /></AppPage>} />
          <Route path="/app/marketplace" element={<AppPage roles={["admin"]}><MarketplacePage /></AppPage>} />
          <Route path="/app/dashboard" element={<AppPage><Dashboard /></AppPage>} />

          <Route path="/admin" element={<Navigate to="/app" replace />} />
          <Route path="/admin/orders" element={<Navigate to="/app/pedidos" replace />} />
          <Route path="/admin/atendimento" element={<Navigate to="/app/atendimento" replace />} />
          <Route path="/admin/atendimento/pedido" element={<Navigate to="/app/atendimento/pedido" replace />} />
          <Route path="/admin/atendimento/clientes" element={<Navigate to="/app/atendimento/clientes" replace />} />
          <Route path="/admin/products" element={<Navigate to="/app/produtos" replace />} />
          <Route path="/admin/customers" element={<Navigate to="/app/clientes" replace />} />
          <Route path="/admin/loyalty" element={<Navigate to="/app/fidelidade" replace />} />
          <Route path="/admin/promotions" element={<Navigate to="/app/promocoes" replace />} />
          <Route path="/admin/routes" element={<Navigate to="/app/logistica/rotas" replace />} />
          <Route path="/admin/routes/planner" element={<Navigate to="/app/logistica/rotas/planner" replace />} />
          <Route path="/admin/deliverers" element={<Navigate to="/app/logistica/entregadores" replace />} />
          <Route path="/admin/deliverers/new" element={<Navigate to="/app/logistica/entregadores/novo" replace />} />
          <Route path="/admin/purchases" element={<Navigate to="/app/compras" replace />} />
          <Route path="/admin/suppliers" element={<Navigate to="/app/fornecedores" replace />} />
          <Route path="/admin/supplies" element={<Navigate to="/app/insumos" replace />} />
          <Route path="/admin/financeiro" element={<Navigate to="/app/financeiro" replace />} />
          <Route path="/admin/financial" element={<Navigate to="/app/financeiro/lancamentos" replace />} />
          <Route path="/admin/pdv/terminais" element={<Navigate to="/app/caixa" replace />} />
          <Route path="/admin/pdv/sessoes" element={<Navigate to="/app/caixa/sessoes" replace />} />
          <Route path="/admin/stock" element={<Navigate to="/app/estoque" replace />} />
          <Route path="/admin/reports" element={<Navigate to="/app/relatorios" replace />} />
          <Route path="/admin/equipe" element={<Navigate to="/app/equipe" replace />} />
          <Route path="/admin/fiscal" element={<Navigate to="/app/fiscal" replace />} />
          <Route path="/admin/accounting-dispatch" element={<Navigate to="/app/configuracoes/contabilidade" replace />} />
          <Route path="/admin/scale" element={<Navigate to="/app/balanca" replace />} />
          <Route path="/admin/print" element={<Navigate to="/app/impressao" replace />} />
          <Route path="/admin/audit" element={<Navigate to="/app/auditoria" replace />} />
          <Route path="/admin/agenda" element={<Navigate to="/app/comissoes" replace />} />

          <Route
            path="/pdv"
            element={
              <AdminGuard>
                <PdvProvider>
                  <PdvPage />
                </PdvProvider>
              </AdminGuard>
            }
          />

          <Route path="/master/login" element={<MasterLogin />} />
          <Route path="/master" element={<MasterGuard><MasterCompanies /></MasterGuard>} />
          <Route path="/master/companies/:id" element={<MasterGuard><MasterCompanyDetail /></MasterGuard>} />
          <Route path="/master/platform/whatsapp" element={<MasterGuard><MasterPlatformWhatsapp /></MasterGuard>} />

          <Route path="/deliverer/login" element={<DelivererLogin />} />
          <Route path="/deliverer" element={<DelivererGuard><DelivererHome /></DelivererGuard>} />
          <Route path="/deliverer/route/:routeId" element={<DelivererGuard><DelivererRouteDetail /></DelivererGuard>} />
        </Routes>
      </Suspense>
    </BrowserRouter>
  );
}
