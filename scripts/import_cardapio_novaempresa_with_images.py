from __future__ import annotations

from datetime import datetime, timezone
import os
from pathlib import Path
from uuid import uuid4

import psycopg
import pypdfium2 as pdfium

import import_cardapio_2025 as base

DEFAULT_DSN = "host=localhost port=5432 dbname=petshop_db user=petshop password=petshop"
DEFAULT_TARGET_COMPANY_SLUG = "novaempresa"
DEFAULT_PDF_PATH = Path(r"c:\Users\maaaa\Downloads\cardapio_2025_C2_ATUALIZADO.pdf")
DEFAULT_IMAGE_DIR = Path(r"d:\DEV\vendApps\backend\Petshop.Api\wwwroot\product-images")

DSN = os.getenv("DATABASE_URL") or os.getenv("CARDAPIO_DSN") or DEFAULT_DSN
TARGET_COMPANY_SLUG = os.getenv("CARDAPIO_TARGET_SLUG", DEFAULT_TARGET_COMPANY_SLUG)
PDF_PATH = Path(os.getenv("CARDAPIO_PDF_PATH", str(DEFAULT_PDF_PATH)))
WWWROOT_PRODUCT_IMAGES = Path(os.getenv("CARDAPIO_IMAGE_DIR", str(DEFAULT_IMAGE_DIR)))

# Categoria -> (pagina_pdf, nome_arquivo)
CATEGORY_IMAGE_SOURCES: dict[str, tuple[int, str]] = {
    "Bebidas Geladas": (3, "cardapio-c2-bebidas-geladas.jpg"),
    "Bebidas Quentes": (5, "cardapio-c2-bebidas-quentes.jpg"),
    "Frappes": (7, "cardapio-c2-frappes.jpg"),
    "Doces & Salgados": (9, "cardapio-c2-doces-salgados.jpg"),
    "Waffles Doces": (11, "cardapio-c2-waffles.jpg"),
    "Waffles Salgados": (11, "cardapio-c2-waffles.jpg"),
    "Tortas": (12, "cardapio-c2-tortas.jpg"),
    "Donuts": (14, "cardapio-c2-donuts.jpg"),
    "Salgados Especiais": (16, "cardapio-c2-salgados-especiais.jpg"),
    "Go Toast": (19, "cardapio-c2-go-toast.jpg"),
    "Sanduiches": (20, "cardapio-c2-sanduiches.jpg"),
}


def extract_category_images() -> dict[str, str]:
    if not PDF_PATH.exists():
        raise FileNotFoundError(f"PDF não encontrado: {PDF_PATH}")

    WWWROOT_PRODUCT_IMAGES.mkdir(parents=True, exist_ok=True)
    pdf = pdfium.PdfDocument(str(PDF_PATH))
    category_url: dict[str, str] = {}

    for category, (page_number, file_name) in CATEGORY_IMAGE_SOURCES.items():
        page = pdf[page_number - 1]
        bitmap = page.render(scale=1.7)
        image = bitmap.to_pil().convert("RGB")
        image.thumbnail((1400, 1400))

        out_path = WWWROOT_PRODUCT_IMAGES / file_name
        image.save(out_path, format="JPEG", quality=86, optimize=True)
        category_url[category] = f"/product-images/{file_name}"

    return category_url


def reset_company_data(cur: psycopg.Cursor, company_id: str) -> None:
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
        """DELETE FROM "CashMovements" WHERE "CashSessionId" IN (SELECT "Id" FROM "CashSessions" WHERE "CompanyId" = %s)""",
        """DELETE FROM "CashSessions" WHERE "CompanyId" = %s""",
        """DELETE FROM "CashRegisterFiscalConfigs" WHERE "CashRegisterId" IN (SELECT "Id" FROM "CashRegisters" WHERE "CompanyId" = %s)""",
        """DELETE FROM "CashRegisters" WHERE "CompanyId" = %s""",
        """DELETE FROM "LoyaltyTransactions" WHERE "CompanyId" = %s""",
        """DELETE FROM "StockMovements" WHERE "CompanyId" = %s""",
        """DELETE FROM "PurchaseOrderItems" WHERE "PurchaseOrderId" IN (SELECT "Id" FROM "PurchaseOrders" WHERE "CompanyId" = %s)""",
        """DELETE FROM "PurchaseOrders" WHERE "CompanyId" = %s""",
        """DELETE FROM "Supplies" WHERE "CompanyId" = %s""",
        """DELETE FROM "Promotions" WHERE "CompanyId" = %s""",
        """DELETE FROM "ProductAddons" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""",
        """DELETE FROM "ProductImages" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""",
        """DELETE FROM "ProductVariants" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""",
        """DELETE FROM "ProductPriceHistories" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""",
        """DELETE FROM "ProductChangeLogs" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""",
        """DELETE FROM "Products" WHERE "CompanyId" = %s""",
        """DELETE FROM "Categories" WHERE "CompanyId" = %s""",
        """DELETE FROM "Brands" WHERE "CompanyId" = %s""",
    ]
    for sql in statements:
        cur.execute(sql, (company_id,))


def run() -> None:
    if not PDF_PATH.exists():
        raise FileNotFoundError(
            f"PDF não encontrado em '{PDF_PATH}'. Defina CARDAPIO_PDF_PATH com o caminho correto."
        )

    category_image_urls = extract_category_images()
    now = datetime.now(timezone.utc)

    with psycopg.connect(DSN) as conn:
        with conn.cursor() as cur:
            cur.execute('SELECT "Id"::text, "Name" FROM "Companies" WHERE "Slug" = %s', (TARGET_COMPANY_SLUG,))
            row = cur.fetchone()
            if row is None:
                raise RuntimeError(f"Empresa com slug '{TARGET_COMPANY_SLUG}' não encontrada.")

            company_id, company_name = row
            print(f"Importando para empresa: {company_name} ({company_id})")

            reset_company_data(cur, company_id)
            print("Reset da empresa concluído.")

            category_map: dict[str, str] = {}
            used_slugs: set[str] = set()

            for cat_name in sorted({p.category for p in base.CATALOG}):
                cat_id = str(uuid4())
                cat_slug = base.slugify(cat_name)
                category_map[cat_name] = cat_id
                cur.execute(
                    """
                    INSERT INTO "Categories" ("Id","CompanyId","Name","Slug")
                    VALUES (%s,%s,%s,%s)
                    """,
                    (cat_id, company_id, cat_name, cat_slug),
                )

            for p in base.CATALOG:
                base_slug = base.slugify(p.name)
                slug = base_slug
                i = 2
                while slug in used_slugs:
                    slug = f"{base_slug}-{i}"
                    i += 1
                used_slugs.add(slug)

                product_id = str(uuid4())
                has_addons = p.with_standard_addons
                image_url = category_image_urls.get(p.category)

                cur.execute(
                    """
                    INSERT INTO "Products"
                        ("Id","CompanyId","Name","Slug","CategoryId","Description","Unit","PriceCents","CostCents",
                         "MarginPercent","StockQty","IsActive","CreatedAtUtc","HasAddons","IsSupply","ImageUrl")
                    VALUES
                        (%s,%s,%s,%s,%s,%s,'UN',%s,0,0,0,true,%s,%s,false,%s)
                    """,
                    (
                        product_id,
                        company_id,
                        p.name,
                        slug,
                        category_map[p.category],
                        p.description if p.description else None,
                        base.to_cents(p.price),
                        now,
                        has_addons,
                        image_url,
                    ),
                )

                if image_url:
                    cur.execute(
                        """
                        INSERT INTO "ProductImages"
                            ("Id","ProductId","Url","StorageProvider","IsPrimary","SortOrder","CreatedAtUtc")
                        VALUES (%s,%s,%s,'Local',true,0,%s)
                        """,
                        (str(uuid4()), product_id, image_url, now),
                    )

                if has_addons:
                    for sort_order, (addon_name, addon_price) in enumerate(base.STANDARD_BEVERAGE_ADDONS, start=1):
                        cur.execute(
                            """
                            INSERT INTO "ProductAddons"
                                ("Id","ProductId","Name","PriceCents","SortOrder","IsActive","CreatedAtUtc")
                            VALUES (%s,%s,%s,%s,%s,true,%s)
                            """,
                            (str(uuid4()), product_id, addon_name, base.to_cents(addon_price), sort_order, now),
                        )

        conn.commit()

    print(f"Importação concluída para '{TARGET_COMPANY_SLUG}' com imagens por categoria.")


if __name__ == "__main__":
    run()
