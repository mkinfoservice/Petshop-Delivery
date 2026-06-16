import re
import unicodedata
from dataclasses import dataclass
from datetime import datetime, timezone
import os
from uuid import uuid4

import psycopg

DEFAULT_DSN = "host=localhost port=5432 dbname=petshop_db user=petshop password=petshop"
DEFAULT_TARGET_COMPANY_SLUG = "novaempresa"

DSN = os.getenv("DATABASE_URL") or os.getenv("CARDAPIO_DSN") or DEFAULT_DSN
TARGET_COMPANY_SLUG = os.getenv("CARDAPIO_TARGET_SLUG", DEFAULT_TARGET_COMPANY_SLUG)
DRY_RUN = (os.getenv("CARDAPIO_DRY_RUN", "0").strip().lower() in {"1", "true", "yes", "on"})


@dataclass
class ProductDef:
    category: str
    name: str
    price: float
    description: str = ""
    with_standard_addons: bool = False


STANDARD_BEVERAGE_ADDONS = [
    ("Chantilly", 4.00),
    ("Marshmallow tostado", 4.00),
    ("Dose de cafe", 4.00),
    ("Cobertura (caramelo ou chocolate)", 3.00),
    ("Leite sem lactose", 3.00),
    ("Leite de aveia", 4.00),
]


CATALOG: list[ProductDef] = [
    # Bebidas Geladas
    ProductDef("Bebidas Geladas", "Latte Ice (P 400ml)", 18.00, "Cafe espresso + leite + syrup + gelo.", True),
    ProductDef("Bebidas Geladas", "Latte Ice (G 500ml)", 20.00, "Cafe espresso + leite + syrup + gelo.", True),
    ProductDef("Bebidas Geladas", "Espresso Tonica (G)", 21.00, "Cafe espresso duplo + agua tonica + syrup + gelo.", True),
    ProductDef("Bebidas Geladas", "Americano Ice (G)", 16.00, "Cafe espresso duplo + agua + gelo.", True),
    ProductDef("Bebidas Geladas", "Matcha Go Coffee (G)", 21.50, "Cha verde + mel + gengibre.", True),
    ProductDef("Bebidas Geladas", "Chai Gelado (G)", 17.50, "Cha preto + especiarias + leite + gelo.", True),
    ProductDef("Bebidas Geladas", "Cha Mate (G)", 14.00, "Natural ou limao/adoçado.", True),
    ProductDef("Bebidas Geladas", "Hibisco Jamaica (G)", 14.00, "Cha de hibisco + gelo.", True),
    ProductDef("Bebidas Geladas", "Go Soda (G)", 14.00, "Agua gaseificada + syrup + gelo.", True),
    ProductDef("Bebidas Geladas", "Go Fresh!", 17.00, "Consulte opcoes.", True),
    ProductDef("Bebidas Geladas", "Refrigerante", 6.50, "Consulte opcoes."),
    ProductDef("Bebidas Geladas", "Agua", 6.00, "Com ou sem gas."),
    # Bebidas Quentes
    ProductDef("Bebidas Quentes", "Espresso GO (P 80ml)", 9.00, "Cafe espresso duplo (60ml).", True),
    ProductDef("Bebidas Quentes", "Espresso Honey (P 80ml)", 11.00, "Cafe espresso duplo + chantilly + mel.", True),
    ProductDef("Bebidas Quentes", "Macchiato (P 80ml)", 11.00, "Cafe espresso duplo + espuma de leite.", True),
    ProductDef("Bebidas Quentes", "Americano (M 250ml)", 10.50, "Cafe espresso duplo + agua quente.", True),
    ProductDef("Bebidas Quentes", "Coado Hario V60 (M 250ml)", 16.00, "Metodo japones coado na hora.", True),
    ProductDef("Bebidas Quentes", "Coado Hario V60 Microlote (M 250ml)", 19.00, "Lotes exclusivos e limitados.", True),
    ProductDef("Bebidas Quentes", "Cafe Latte (M 250ml)", 13.00, "Cafe espresso + leite vaporizado.", True),
    ProductDef("Bebidas Quentes", "Cafe Latte (G 400ml)", 15.00, "Cafe espresso + leite vaporizado.", True),
    ProductDef("Bebidas Quentes", "Cafe Mocha (M 250ml)", 16.50, "Cafe espresso + leite + chocolate.", True),
    ProductDef("Bebidas Quentes", "Cafe Mocha (G 400ml)", 19.00, "Cafe espresso + leite + chocolate.", True),
    ProductDef("Bebidas Quentes", "Cafe Carmelotto (M 250ml)", 16.50, "Cafe espresso + leite + caramelo.", True),
    ProductDef("Bebidas Quentes", "Cafe Carmelotto (G 400ml)", 19.00, "Cafe espresso + leite + caramelo.", True),
    ProductDef("Bebidas Quentes", "Ultra Mocha (G 400ml)", 22.00, "Cafe espresso + leite + dobro de chocolate + chantilly.", True),
    ProductDef("Bebidas Quentes", "Ultra Carmelotto (G 400ml)", 22.00, "Cafe espresso + leite + dobro de caramelo + chantilly.", True),
    ProductDef("Bebidas Quentes", "Cappuccino Italiano (M 250ml)", 14.00, "Cafe espresso + leite vaporizado + canela.", True),
    ProductDef("Bebidas Quentes", "Cappuccino Italiano (G 400ml)", 17.00, "Cafe espresso + leite vaporizado + canela.", True),
    ProductDef("Bebidas Quentes", "Cappuccino Brasileiro (M 250ml)", 15.50, "Cafe + leite + chocolate + canela.", True),
    ProductDef("Bebidas Quentes", "Cappuccino Brasileiro (G 400ml)", 18.00, "Cafe + leite + chocolate + canela.", True),
    ProductDef("Bebidas Quentes", "Cappuccino Go Coffee (G 400ml)", 22.00, "Cafe espresso + leite + caramelo + canela + chantilly.", True),
    ProductDef("Bebidas Quentes", "Chai Latte (M 250ml)", 15.50, "Cha preto + especiarias indianas + leite vaporizado.", True),
    ProductDef("Bebidas Quentes", "Chai Latte (G 400ml)", 17.00, "Cha preto + especiarias indianas + leite vaporizado.", True),
    ProductDef("Bebidas Quentes", "Chocolate Quente (M 250ml)", 16.00, "Chocolate quente cremoso.", True),
    ProductDef("Bebidas Quentes", "Chocolate Quente (G 400ml)", 18.50, "Chocolate quente cremoso.", True),
    ProductDef("Bebidas Quentes", "Chocomallow Gocoffee (M 250ml)", 20.00, "Chocolate + marshmallow tostado.", True),
    ProductDef("Bebidas Quentes", "Chocomallow Gocoffee (G 400ml)", 22.00, "Chocolate + marshmallow tostado.", True),
    # Frappes
    ProductDef("Frappes", "Pacoca (P 400ml)", 21.00, "Base cafe ou creme.", True),
    ProductDef("Frappes", "Pacoca (G 500ml)", 24.00, "Base cafe ou creme.", True),
    ProductDef("Frappes", "Banoffee (P 400ml)", 21.00, "Base cafe ou creme.", True),
    ProductDef("Frappes", "Banoffee (G 500ml)", 24.00, "Base cafe ou creme.", True),
    ProductDef("Frappes", "Cappuccino (P 400ml)", 21.00, "Base cafe.", True),
    ProductDef("Frappes", "Cappuccino (G 500ml)", 24.00, "Base cafe.", True),
    ProductDef("Frappes", "Red (P 400ml)", 21.00, "Morango base creme.", True),
    ProductDef("Frappes", "Red (G 500ml)", 24.00, "Morango base creme.", True),
    ProductDef("Frappes", "Vanilla Macchiato (P 400ml)", 21.00, "Baunilha.", True),
    ProductDef("Frappes", "Vanilla Macchiato (G 500ml)", 24.00, "Baunilha.", True),
    ProductDef("Frappes", "Avela (P 400ml)", 21.00, "Avela + cacau.", True),
    ProductDef("Frappes", "Avela (G 500ml)", 24.00, "Avela + cacau.", True),
    ProductDef("Frappes", "Chocolate (P 400ml)", 21.00, "Chocolate.", True),
    ProductDef("Frappes", "Chocolate (G 500ml)", 24.00, "Chocolate.", True),
    ProductDef("Frappes", "Caramel (P 400ml)", 21.00, "Caramelo.", True),
    ProductDef("Frappes", "Caramel (G 500ml)", 24.00, "Caramelo.", True),
    ProductDef("Frappes", "Caramelo Salgado (G 500ml)", 24.00, "Caramelo salgado.", True),
    ProductDef("Frappes", "Cream Chips (P 400ml)", 21.00, "Baunilha + gotas de chocolate.", True),
    ProductDef("Frappes", "Cream Chips (G 500ml)", 24.00, "Baunilha + gotas de chocolate.", True),
    ProductDef("Frappes", "Frappe do Dia (P 400ml)", 18.00, "Segunda a sexta. Consulte o barista.", True),
    ProductDef("Frappes", "Frappe Secreto (G 500ml)", 25.00, "Sazonal. Consulte o barista.", True),
    ProductDef("Frappes", "Frappe Especial (G 500ml)", 30.00, "Sazonal. Consulte o barista.", True),
    # Doces & Salgados
    ProductDef("Doces & Salgados", "Cookie Go Coffee", 18.00, "Consulte sabores disponiveis."),
    ProductDef("Doces & Salgados", "Muffin", 14.00, "Consulte sabores disponiveis."),
    ProductDef("Doces & Salgados", "Brownie", 15.00, ""),
    ProductDef("Doces & Salgados", "Brownie extra chocolate", 20.00, "Brownie com ultra calda de chocolate, chantilly e cacau."),
    ProductDef("Doces & Salgados", "Brownie caramelo com pacoca", 20.00, "Brownie com calda de caramelo, chantilly e pacoca."),
    ProductDef("Doces & Salgados", "Croissant doce", 17.50, "Consulte opcoes e sabores disponiveis."),
    ProductDef("Doces & Salgados", "Pao de queijo", 7.50, ""),
    ProductDef("Doces & Salgados", "Mini paes de queijo", 13.00, "Porcao com 10 unidades."),
    ProductDef("Doces & Salgados", "Salgados", 14.00, "Consulte opcoes disponiveis."),
    # Waffles
    ProductDef("Waffles Doces", "Waffle Banoffee", 20.00, "Banana, doce de leite e chantilly."),
    ProductDef("Waffles Doces", "Waffle Chocolate ao Leite", 20.00, "Chocolate ao leite e chantilly."),
    ProductDef("Waffles Doces", "Waffle Chocolate Branco", 20.00, "Chocolate branco e chantilly."),
    ProductDef("Waffles Doces", "Waffle Frutas Vermelhas", 20.00, "Calda e pedacos de frutas vermelhas."),
    ProductDef("Waffles Doces", "Waffle Mel e Granola", 20.00, "Mel e granola."),
    ProductDef("Waffles Salgados", "Waffle Queijo", 20.00, "Requeijao e queijo mussarela."),
    ProductDef("Waffles Salgados", "Waffle Queijo e Presunto", 20.00, "Requeijao, queijo e presunto."),
    ProductDef("Waffles Salgados", "Waffle Queijo e Peito de Peru", 20.00, "Requeijao, queijo e peito de peru."),
    # Tortas
    ProductDef("Tortas", "Tortas tradicionais", 19.00, "Pop Slice. Consulte opcoes e sabores."),
    ProductDef("Tortas", "Tortas especiais", 22.00, "Pop Slice. Consulte opcoes e sabores."),
    # Donuts / Salgados especiais
    ProductDef("Donuts", "Donuts", 17.00, "Consulte opcoes e sabores disponiveis."),
    ProductDef("Salgados Especiais", "Salgados Especiais", 17.50, "Consulte opcoes e sabores disponiveis."),
    # Go Toast
    ProductDef("Go Toast", "Go Toast Manteiga", 15.00, "Pao com manteiga na chapa."),
    ProductDef("Go Toast", "Pao de Batata Manteiga", 16.00, "Pao de batata com manteiga na chapa."),
    ProductDef("Go Toast", "Go Toast Queijo", 18.00, "Pao com manteiga na chapa e queijo."),
    ProductDef("Go Toast", "Pao de Batata Queijo", 19.00, "Pao de batata com queijo."),
    ProductDef("Go Toast", "Go Toast Mix", 19.50, "Pao com manteiga na chapa recheado com queijo e presunto."),
    ProductDef("Go Toast", "Pao de Batata Mix", 21.00, "Pao de batata recheado com queijo e presunto."),
    ProductDef("Go Toast", "Go Toast Recheados", 17.50, "Escolha entre opcoes de recheios especiais."),
    # Sanduiches
    ProductDef("Sanduiches", "Mediterraneo", 24.00, "Cream cheese, cheddar, tomate cereja, manjericao e azeite."),
    ProductDef("Sanduiches", "Pesto", 24.00, "Pesto, peito de peru, cheddar, rucula e azeite."),
]


def slugify(text: str) -> str:
    text = unicodedata.normalize("NFKD", text).encode("ascii", "ignore").decode("ascii")
    text = re.sub(r"[^a-zA-Z0-9]+", "-", text.lower()).strip("-")
    return text or "item"


def to_cents(v: float) -> int:
    return int(round(v * 100))


def reset_company_data(cur: psycopg.Cursor, company_id: str) -> None:
    # Limpeza completa da empresa alvo, preservando:
    # - admins/master admins e acessos
    # - integrações/configurações (WhatsApp, CompanySettings, StoreFrontConfig, etc.)
    # - configuração estrutural de caixa (CashRegisters e CashRegisterFiscalConfigs)
    statements = [
        """
        DELETE FROM "SaleOrderItemAddons"
        WHERE "SaleOrderItemId" IN (
            SELECT soi."Id"
            FROM "SaleOrderItems" soi
            JOIN "SaleOrders" so ON so."Id" = soi."SaleOrderId"
            WHERE so."CompanyId" = %s
        )
        """,
        """DELETE FROM "SalePayments" WHERE "SaleOrderId" IN (SELECT "Id" FROM "SaleOrders" WHERE "CompanyId" = %s)""",
        """DELETE FROM "SaleOrderItems" WHERE "SaleOrderId" IN (SELECT "Id" FROM "SaleOrders" WHERE "CompanyId" = %s)""",
        """DELETE FROM "FiscalDocuments" WHERE "SaleOrderId" IN (SELECT "Id" FROM "SaleOrders" WHERE "CompanyId" = %s)""",
        """DELETE FROM "FiscalQueues" WHERE "SaleOrderId" IN (SELECT "Id" FROM "SaleOrders" WHERE "CompanyId" = %s)""",
        """DELETE FROM "SaleOrders" WHERE "CompanyId" = %s""",
        """DELETE FROM "SalesQuoteItems" WHERE "SalesQuoteId" IN (SELECT "Id" FROM "SalesQuotes" WHERE "CompanyId" = %s)""",
        """DELETE FROM "SalesQuotes" WHERE "CompanyId" = %s""",
        """DELETE FROM "PrintJobs" WHERE "OrderId" IN (SELECT "Id" FROM "Orders" WHERE "CompanyId" = %s)""",
        """DELETE FROM "OrderItems" WHERE "OrderId" IN (SELECT "Id" FROM "Orders" WHERE "CompanyId" = %s)""",
        """DELETE FROM "RouteStops" WHERE "OrderId" IN (SELECT "Id" FROM "Orders" WHERE "CompanyId" = %s)""",
        """DELETE FROM "Orders" WHERE "CompanyId" = %s""",
        """
        DELETE FROM "Routes"
        WHERE "Id" IN (
            SELECT r."Id"
            FROM "Routes" r
            LEFT JOIN "RouteStops" rs ON rs."RouteId" = r."Id"
            GROUP BY r."Id"
            HAVING COUNT(rs."Id") = 0
        )
        """,
        """DELETE FROM "CashMovements" WHERE "CashSessionId" IN (SELECT "Id" FROM "CashSessions" WHERE "CompanyId" = %s)""",
        """DELETE FROM "CashSessions" WHERE "CompanyId" = %s""",
        """DELETE FROM "LoyaltyTransactions" WHERE "CompanyId" = %s""",
        """DELETE FROM "StockMovements" WHERE "CompanyId" = %s""",
        """DELETE FROM "PurchaseOrderItems" WHERE "PurchaseOrderId" IN (SELECT "Id" FROM "PurchaseOrders" WHERE "CompanyId" = %s)""",
        """DELETE FROM "PurchaseOrders" WHERE "CompanyId" = %s""",
        """DELETE FROM "Supplies" WHERE "CompanyId" = %s""",
        """DELETE FROM "Promotions" WHERE "CompanyId" = %s""",
        """DELETE FROM "ServiceAppointments" WHERE "CompanyId" = %s""",
        """DELETE FROM "FinancialEntries" WHERE "CompanyId" = %s""",
        """DELETE FROM "AdminAlerts" WHERE "CompanyId" = %s""",
        """DELETE FROM "WhatsAppMessageLogs" WHERE "CompanyId" = %s""",
        """DELETE FROM "WhatsAppContacts" WHERE "CompanyId" = %s""",
        """DELETE FROM "ProductAddons" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""",
        """DELETE FROM "ProductImages" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""",
        """DELETE FROM "ProductVariants" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""",
        """DELETE FROM "ProductPriceHistories" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""",
        """DELETE FROM "ProductChangeLogs" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""",
        """DELETE FROM "ProductNameSuggestions" WHERE "CompanyId" = %s""",
        """DELETE FROM "ProductImageCandidates" WHERE "CompanyId" = %s""",
        """DELETE FROM "ProductEnrichmentResults" WHERE "CompanyId" = %s""",
        """DELETE FROM "EnrichmentBatches" WHERE "CompanyId" = %s""",
        """
        DELETE FROM "ProductSyncItems"
        WHERE "JobId" IN (
            SELECT "Id" FROM "ProductSyncJobs" WHERE "CompanyId" = %s
        )
        """,
        """DELETE FROM "ProductSyncJobs" WHERE "CompanyId" = %s""",
        """DELETE FROM "ExternalProductSnapshots" WHERE "CompanyId" = %s""",
        """DELETE FROM "Products" WHERE "CompanyId" = %s""",
        """DELETE FROM "Categories" WHERE "CompanyId" = %s""",
        """DELETE FROM "Brands" WHERE "CompanyId" = %s""",
        """DELETE FROM "Customers" WHERE "CompanyId" = %s""",
    ]
    for sql in statements:
        if "%s" in sql:
            cur.execute(sql, (company_id,))
        else:
            cur.execute(sql)


def run() -> None:
    now = datetime.now(timezone.utc)
    with psycopg.connect(DSN) as conn:
        with conn.cursor() as cur:
            cur.execute('SELECT "Id"::text, "Name" FROM "Companies" WHERE "Slug" = %s', (TARGET_COMPANY_SLUG,))
            row = cur.fetchone()
            if row is None:
                raise RuntimeError(f"Empresa com slug '{TARGET_COMPANY_SLUG}' nao encontrada.")

            company_id, company_name = row
            print(f"Importando para empresa: {company_name} ({company_id})")

            reset_company_data(cur, company_id)
            print("Reset da empresa concluido.")

            category_map: dict[str, str] = {}
            used_slugs: set[str] = set()

            for cat_name in sorted({p.category for p in CATALOG}):
                cat_id = str(uuid4())
                cat_slug = slugify(cat_name)
                category_map[cat_name] = cat_id
                cur.execute(
                    """
                    INSERT INTO "Categories" ("Id","CompanyId","Name","Slug")
                    VALUES (%s,%s,%s,%s)
                    """,
                    (cat_id, company_id, cat_name, cat_slug),
                )

            for p in CATALOG:
                base_slug = slugify(p.name)
                slug = base_slug
                i = 2
                while slug in used_slugs:
                    slug = f"{base_slug}-{i}"
                    i += 1
                used_slugs.add(slug)

                product_id = str(uuid4())
                has_addons = p.with_standard_addons
                cur.execute(
                    """
                    INSERT INTO "Products"
                        ("Id","CompanyId","Name","Slug","CategoryId","Description","Unit","PriceCents","CostCents",
                         "MarginPercent","StockQty","IsActive","CreatedAtUtc","HasAddons","IsSupply")
                    VALUES
                        (%s,%s,%s,%s,%s,%s,'UN',%s,0,0,0,true,%s,%s,false)
                    """,
                    (
                        product_id,
                        company_id,
                        p.name,
                        slug,
                        category_map[p.category],
                        p.description if p.description else None,
                        to_cents(p.price),
                        now,
                        has_addons,
                    ),
                )

                if has_addons:
                    for sort_order, (addon_name, addon_price) in enumerate(STANDARD_BEVERAGE_ADDONS, start=1):
                        cur.execute(
                            """
                            INSERT INTO "ProductAddons"
                                ("Id","ProductId","Name","PriceCents","SortOrder","IsActive","CreatedAtUtc")
                            VALUES (%s,%s,%s,%s,%s,true,%s)
                            """,
                            (str(uuid4()), product_id, addon_name, to_cents(addon_price), sort_order, now),
                        )

            print(f"Empresa '{company_name}': {len(category_map)} categorias, {len(CATALOG)} produtos.")

            if DRY_RUN:
                cur.execute('SELECT COUNT(*) FROM "Categories" WHERE "CompanyId" = %s', (company_id,))
                cat_count = cur.fetchone()[0]
                cur.execute('SELECT COUNT(*) FROM "Products" WHERE "CompanyId" = %s', (company_id,))
                product_count = cur.fetchone()[0]
                cur.execute('SELECT COUNT(*) FROM "ProductAddons" pa JOIN "Products" p ON p."Id" = pa."ProductId" WHERE p."CompanyId" = %s', (company_id,))
                addon_count = cur.fetchone()[0]
                print(f"[DRY-RUN] Resultado simulado -> categorias: {cat_count}, produtos: {product_count}, adicionais: {addon_count}.")
                conn.rollback()
                print("[DRY-RUN] Rollback executado. Nenhuma alteracao foi persistida.")
                return

        conn.commit()
    print("Importacao finalizada com sucesso.")


if __name__ == "__main__":
    run()
