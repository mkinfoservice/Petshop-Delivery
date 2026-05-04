"""
import_gocoffee.py
==================
Importa o cardápio completo da Go Coffee (C2) para uma empresa do VendApps.

USO:
    python import_gocoffee.py                          # novaempresa, local
    CARDAPIO_TARGET_SLUG=gocoffee python import_gocoffee.py
    CARDAPIO_DRY_RUN=1 python import_gocoffee.py       # simulação sem commit

VARIÁVEIS DE AMBIENTE:
    CARDAPIO_DSN          — DSN do banco (padrão: local)
    CARDAPIO_TARGET_SLUG  — slug da empresa alvo (padrão: novaempresa)
    CARDAPIO_DRY_RUN      — 1/true para rollback no final
"""

import re
import unicodedata
from dataclasses import dataclass, field
from datetime import datetime, timezone
from uuid import uuid4
import os

import psycopg

# ── Configuração ─────────────────────────────────────────────────────────────
DEFAULT_DSN = "host=localhost port=5432 dbname=petshop_db user=petshop password=petshop"
DEFAULT_TARGET_SLUG = "novaempresa"

DSN = os.getenv("DATABASE_URL") or os.getenv("CARDAPIO_DSN") or DEFAULT_DSN
TARGET_SLUG = os.getenv("CARDAPIO_TARGET_SLUG", DEFAULT_TARGET_SLUG)
DRY_RUN = os.getenv("CARDAPIO_DRY_RUN", "0").strip().lower() in {"1", "true", "yes", "on"}


# ── Helpers ──────────────────────────────────────────────────────────────────
def slugify(text: str) -> str:
    text = unicodedata.normalize("NFKD", text).encode("ascii", "ignore").decode("ascii")
    text = re.sub(r"[^a-zA-Z0-9]+", "-", text.lower()).strip("-")
    return text or "item"


def cents(v: float) -> int:
    return int(round(v * 100))


# ── Tipos de dados ────────────────────────────────────────────────────────────
@dataclass
class Variant:
    """Variante de tamanho ou tipo (ex: P 400ml, G 500ml, Pão de Batata)."""
    key: str         # "Tamanho" | "Tipo de pão"
    value: str       # "P 400ml" | "G 500ml" | "Pão de Batata"
    price: float     # preço desta variante


@dataclass
class Addon:
    name: str
    price: float


@dataclass
class ProductDef:
    category: str
    name: str
    description: str
    price: float                         # preço base (menor variante ou único)
    variants: list[Variant] = field(default_factory=list)
    addons: list[Addon] = field(default_factory=list)


# ── Adicionais padrão ─────────────────────────────────────────────────────────
BEVERAGE_ADDONS = [
    Addon("Chantilly", 4.00),
    Addon("Marshmallow tostado", 4.00),
    Addon("Dose de café", 4.00),
    Addon("Cobertura (caramelo ou chocolate)", 3.00),
    Addon("Leite sem lactose", 3.00),
    Addon("Leite de aveia", 4.00),
]


def bev_addons(*flavor_options: str) -> list[Addon]:
    """Retorna adicionais padrão + opções de sabor a R$0."""
    flavors = [Addon(f, 0.00) for f in flavor_options]
    return flavors + BEVERAGE_ADDONS


def T(*args) -> list[Variant]:
    """Atalho para variantes de Tamanho: T('P 400ml', 18.0, 'G 500ml', 20.0)"""
    assert len(args) % 2 == 0
    return [Variant("Tamanho", args[i], args[i + 1]) for i in range(0, len(args), 2)]


def P(*args) -> list[Variant]:
    """Variantes de Tipo de pão: P('Go Toast', 15.0, 'Pão de Batata', 16.0)"""
    assert len(args) % 2 == 0
    return [Variant("Tipo de pão", args[i], args[i + 1]) for i in range(0, len(args), 2)]


# ── Catálogo completo ─────────────────────────────────────────────────────────
CATALOG: list[ProductDef] = [

    # ═══════════════════════════════════════════════════
    # 1. Bebidas Geladas
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Bebidas Geladas", "Latte Ice",
        "Café espresso + leite + syrup + gelo. "
        "Sabores: Natural, Baunilha, Avelã (com cobertura de chocolate), "
        "Caramelo (com cobertura de caramelo), Caramelo Salgado (com cobertura de caramelo).",
        price=18.00,
        variants=T("P 400ml", 18.00, "G 500ml", 20.00),
        addons=bev_addons("Natural", "Baunilha", "Avelã", "Caramelo", "Caramelo Salgado"),
    ),
    ProductDef(
        "Bebidas Geladas", "Espresso Tônica",
        "Café espresso duplo + água tônica + syrup + gelo. Sabores: Natural, Romã, Cereja.",
        price=21.00,
        variants=T("G 500ml", 21.00),
        addons=bev_addons("Natural", "Romã", "Cereja"),
    ),
    ProductDef(
        "Bebidas Geladas", "Americano Ice",
        "Café espresso duplo + água + gelo.",
        price=16.00,
        variants=T("G 500ml", 16.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Geladas", "Matcha Go Coffee",
        "Chá verde + mel + gengibre. Preparado com leite ou água (adoçado).",
        price=21.50,
        variants=T("G 500ml", 21.50),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Geladas", "Chai Gelado",
        "Chá preto + especiarias indianas + leite + gelo (adoçado).",
        price=17.50,
        variants=T("G 500ml", 17.50),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Geladas", "Chá Mate",
        "Chá mate + gelo. Natural ou limão/adoçado.",
        price=14.00,
        variants=T("G 500ml", 14.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Geladas", "Hibisco Jamaica",
        "Chá natural de hibisco + gelo. Opcional: syrup de limão (adoçado).",
        price=14.00,
        variants=T("G 500ml", 14.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Geladas", "Go Soda",
        "Água gaseificada + syrup + gelo. "
        "Sabores: Romã, Cereja, Limão siciliano, Maçã verde, Morango.",
        price=14.00,
        variants=T("G 500ml", 14.00),
        addons=bev_addons("Romã", "Cereja", "Limão siciliano", "Maçã verde", "Morango"),
    ),
    ProductDef(
        "Bebidas Geladas", "Go Fresh!",
        "Bebida refrescante especial. Consulte as opções disponíveis.",
        price=17.00,
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Geladas", "Refrigerante",
        "Consulte as opções e sabores disponíveis.",
        price=6.50,
    ),
    ProductDef(
        "Bebidas Geladas", "Água",
        "Com ou sem gás.",
        price=6.00,
    ),

    # ═══════════════════════════════════════════════════
    # 2. Bebidas Quentes
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Bebidas Quentes", "Espresso GO",
        "Café espresso duplo (60ml). Encorpado e intenso.",
        price=9.00,
        variants=T("P 80ml", 9.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Espresso Honey",
        "Café espresso duplo + chantilly + mel.",
        price=11.00,
        variants=T("P 80ml", 11.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Macchiato",
        "Café espresso duplo + espuma de leite.",
        price=11.00,
        variants=T("P 80ml", 11.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Americano",
        "Café espresso duplo + água quente.",
        price=10.50,
        variants=T("M 250ml", 10.50),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Coado Hario V60",
        "Método japonês que acentua acidez e doçura natural do grão (coado na hora).",
        price=16.00,
        variants=T("M 250ml", 16.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Coado Hario V60 Microlote",
        "Lotes exclusivos e limitados. Café com perfil sensorial único. "
        "Consulte o microlote disponível.",
        price=19.00,
        variants=T("M 250ml", 19.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Café Latte",
        "Café espresso + leite vaporizado.",
        price=13.00,
        variants=T("M 250ml", 13.00, "G 400ml", 15.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Café Mocha",
        "Café espresso + leite vaporizado + chocolate.",
        price=16.50,
        variants=T("M 250ml", 16.50, "G 400ml", 19.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Café Carmelotto",
        "Café espresso + leite vaporizado + caramelo.",
        price=16.50,
        variants=T("M 250ml", 16.50, "G 400ml", 19.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Ultra Mocha",
        "Café espresso duplo + leite vaporizado + dobro de chocolate + chantilly.",
        price=22.00,
        variants=T("G 400ml", 22.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Ultra Carmelotto",
        "Café espresso duplo + leite vaporizado + dobro de caramelo + chantilly.",
        price=22.00,
        variants=T("G 400ml", 22.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Cappuccino Italiano",
        "Café espresso duplo + leite vaporizado + canela (opcional).",
        price=14.00,
        variants=T("M 250ml", 14.00, "G 400ml", 17.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Cappuccino Brasileiro",
        "Café + leite vaporizado + chocolate + canela (adoçado).",
        price=15.50,
        variants=T("M 250ml", 15.50, "G 400ml", 18.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Cappuccino Go Coffee",
        "Café espresso + leite vaporizado + caramelo + canela + chantilly.",
        price=22.00,
        variants=T("G 400ml", 22.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Chai Latte",
        "Chá preto + especiarias indianas + leite vaporizado (adoçado).",
        price=15.50,
        variants=T("M 250ml", 15.50, "G 400ml", 17.00),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Chocolate Quente",
        "Chocolate quente cremoso.",
        price=16.00,
        variants=T("M 250ml", 16.00, "G 400ml", 18.50),
        addons=bev_addons(),
    ),
    ProductDef(
        "Bebidas Quentes", "Chocomallow Gocoffee",
        "Chocolate quente cremoso + marshmallow tostado.",
        price=20.00,
        variants=T("M 250ml", 20.00, "G 400ml", 22.00),
        addons=bev_addons(),
    ),

    # ═══════════════════════════════════════════════════
    # 3. Frappes
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Frappes", "Frappe Paçoca",
        "Frappe de paçoca de amendoim. "
        "Base café ou creme + syrup + leite + gelo + chantilly + cobertura.",
        price=21.00,
        variants=T("P 400ml", 21.00, "G 500ml", 24.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Banoffee",
        "Frappe de doce de leite + banana + canela. "
        "Base café ou creme + syrup + leite + gelo + chantilly + cobertura.",
        price=21.00,
        variants=T("P 400ml", 21.00, "G 500ml", 24.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Cappuccino",
        "Frappe de chocolate + canela (base café). "
        "Base café + syrup + leite + gelo + chantilly + cobertura.",
        price=21.00,
        variants=T("P 400ml", 21.00, "G 500ml", 24.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Red",
        "Frappe de morango (base creme). "
        "Base creme + syrup + leite + gelo + chantilly + cobertura.",
        price=21.00,
        variants=T("P 400ml", 21.00, "G 500ml", 24.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Vanilla Macchiato",
        "Frappe de baunilha. "
        "Base café ou creme + syrup + leite + gelo + chantilly + cobertura.",
        price=21.00,
        variants=T("P 400ml", 21.00, "G 500ml", 24.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Avelã",
        "Frappe de avelã + cacau. "
        "Base café ou creme + syrup + leite + gelo + chantilly + cobertura.",
        price=21.00,
        variants=T("P 400ml", 21.00, "G 500ml", 24.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Chocollate",
        "Frappe de chocolate. "
        "Base café ou creme + syrup + leite + gelo + chantilly + cobertura.",
        price=21.00,
        variants=T("P 400ml", 21.00, "G 500ml", 24.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Caramel",
        "Frappe de caramelo. "
        "Base café ou creme + syrup + leite + gelo + chantilly + cobertura.",
        price=21.00,
        variants=T("P 400ml", 21.00, "G 500ml", 24.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Caramelo Salgado",
        "Frappe de caramelo salgado. "
        "Base café ou creme + syrup + leite + gelo + chantilly + cobertura.",
        price=24.00,
        variants=T("G 500ml", 24.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Cream Chips",
        "Frappe de baunilha + gotas de chocolate. "
        "Base café ou creme + syrup + leite + gelo + chantilly + cobertura.",
        price=21.00,
        variants=T("P 400ml", 21.00, "G 500ml", 24.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe do Dia",
        "Frappe especial do dia. Segunda a Sexta. Consulte o barista.",
        price=18.00,
        variants=T("P 400ml", 18.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Secreto",
        "Frappe sazonal surpresa. Consulte o barista.",
        price=25.00,
        variants=T("G 500ml", 25.00),
        addons=BEVERAGE_ADDONS,
    ),
    ProductDef(
        "Frappes", "Frappe Especial",
        "Frappe especial sazonal. Consulte o barista.",
        price=30.00,
        variants=T("G 500ml", 30.00),
        addons=BEVERAGE_ADDONS,
    ),

    # ═══════════════════════════════════════════════════
    # 4. Doces
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Doces", "Cookie Go Coffee",
        "Cookie artesanal Go Coffee. Consulte os sabores disponíveis.",
        price=18.00,
    ),
    ProductDef(
        "Doces", "Muffin",
        "Muffin artesanal. Consulte os sabores disponíveis.",
        price=14.00,
    ),
    ProductDef(
        "Doces", "Brownie",
        "Brownie clássico Go Coffee.",
        price=15.00,
    ),
    ProductDef(
        "Doces", "Brownie Extra Chocolate",
        "Brownie coberto com ultra calda de chocolate, chantilly e cacau.",
        price=20.00,
    ),
    ProductDef(
        "Doces", "Brownie Caramelo com Paçoca",
        "Brownie coberto com calda de caramelo, chantilly e paçoca.",
        price=20.00,
    ),
    ProductDef(
        "Doces", "Croissant Doce",
        "Croissant doce recheado. Consulte as opções e sabores disponíveis.",
        price=17.50,
    ),

    # ═══════════════════════════════════════════════════
    # 5. Salgados
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Salgados", "Pão de Queijo",
        "Pão de queijo artesanal Go Coffee.",
        price=7.50,
    ),
    ProductDef(
        "Salgados", "Mini Pães de Queijo",
        "Porção com 10 unidades de mini pão de queijo. Consulte disponibilidade.",
        price=13.00,
    ),
    ProductDef(
        "Salgados", "Salgados",
        "Uma variedade de salgados deliciosos, perfeitos para qualquer hora. "
        "Consulte as opções disponíveis.",
        price=14.00,
    ),

    # ═══════════════════════════════════════════════════
    # 6. Waffles Doces
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Waffles Doces", "Waffle Banoffee",
        "Waffle com banana, doce de leite e chantilly. Finalizado com canela.",
        price=20.00,
    ),
    ProductDef(
        "Waffles Doces", "Waffle Chocolate ao Leite",
        "Waffle com chocolate ao leite e chantilly. Finalizado com cacau.",
        price=20.00,
    ),
    ProductDef(
        "Waffles Doces", "Waffle Chocolate Branco",
        "Waffle com chocolate branco e chantilly.",
        price=20.00,
    ),
    ProductDef(
        "Waffles Doces", "Waffle Frutas Vermelhas",
        "Waffle coberto com calda e pedaços de frutas vermelhas e chantilly.",
        price=20.00,
    ),
    ProductDef(
        "Waffles Doces", "Waffle Mel e Granola",
        "Waffle coberto com mel e granola.",
        price=20.00,
    ),

    # ═══════════════════════════════════════════════════
    # 7. Waffles Salgados
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Waffles Salgados", "Waffle Queijo",
        "Waffle recheado com requeijão e queijo mussarela. "
        "Finalizado com queijo mussarela maçaricado.",
        price=20.00,
    ),
    ProductDef(
        "Waffles Salgados", "Waffle Queijo e Presunto",
        "Waffle recheado com requeijão, queijo e presunto. "
        "Finalizado com queijo mussarela maçaricado. Uma combinação clássica.",
        price=20.00,
    ),
    ProductDef(
        "Waffles Salgados", "Waffle Queijo e Peito de Peru",
        "Waffle recheado com requeijão, queijo e peito de peru. "
        "Finalizado com queijo mussarela maçaricado.",
        price=20.00,
    ),

    # ═══════════════════════════════════════════════════
    # 8. Tortas
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Tortas", "Pop Slice — Torta Tradicional",
        "Fatia de torta tradicional Go Coffee. "
        "Consulte as opções e sabores disponíveis (Rainbow, Chococake, Chocoleite, Pop Italiana, Pop Berries e outros).",
        price=19.00,
    ),
    ProductDef(
        "Tortas", "Pop Slice — Torta Especial",
        "Fatia de torta especial Go Coffee. "
        "Consulte as opções e sabores especiais disponíveis.",
        price=22.00,
    ),

    # ═══════════════════════════════════════════════════
    # 9. Donuts
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Donuts", "Donut",
        "Donut artesanal Go Coffee. Recheios disponíveis: Creme de Avelã, Doce de Leite, "
        "Bavarian, Frutas Vermelhas e outros. Consulte as opções e sabores disponíveis.",
        price=17.00,
    ),

    # ═══════════════════════════════════════════════════
    # 10. Salgados Especiais
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Salgados Especiais", "Salgado Especial",
        "Salgados especiais Go Coffee, feitos com ingredientes selecionados. "
        "Inclui opções como quiches, tortinhas e outros. Consulte as opções disponíveis.",
        price=17.50,
    ),

    # ═══════════════════════════════════════════════════
    # 11. Go Toast
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Go Toast", "Go Toast Manteiga",
        "Pão com manteiga na chapa. Escolha o tipo de pão: Go Toast ou Pão de Batata.",
        price=15.00,
        variants=P("Go Toast", 15.00, "Pão de Batata", 16.00),
    ),
    ProductDef(
        "Go Toast", "Go Toast Queijo",
        "Pão com manteiga na chapa e queijo. "
        "Escolha o tipo de pão: Go Toast ou Pão de Batata.",
        price=18.00,
        variants=P("Go Toast", 18.00, "Pão de Batata", 19.00),
    ),
    ProductDef(
        "Go Toast", "Go Toast Mix",
        "Pão com manteiga na chapa, recheado com queijo e presunto. "
        "Escolha o tipo de pão: Go Toast ou Pão de Batata.",
        price=19.50,
        variants=P("Go Toast", 19.50, "Pão de Batata", 21.00),
    ),
    ProductDef(
        "Go Toast", "Go Toast Recheados",
        "Pão recheado com opções de recheios especiais. Consulte os recheios disponíveis.",
        price=17.50,
        variants=P("Go Toast", 17.50),
    ),

    # ═══════════════════════════════════════════════════
    # 12. Sanduíches
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Sanduíches", "Sanduíche Mediterrâneo",
        "Pão com cream cheese, queijo cheddar, tomate cereja, manjericão e azeite de oliva.",
        price=24.00,
    ),
    ProductDef(
        "Sanduíches", "Sanduíche Pesto",
        "Pão com pesto, peito de peru, queijo cheddar, rúcula e azeite de oliva.",
        price=24.00,
    ),
]

# Ordem das categorias conforme o cardápio Go Coffee C2
CATEGORY_ORDER = [
    "Bebidas Geladas",
    "Bebidas Quentes",
    "Frappes",
    "Doces",
    "Salgados",
    "Waffles Doces",
    "Waffles Salgados",
    "Tortas",
    "Donuts",
    "Salgados Especiais",
    "Go Toast",
    "Sanduíches",
]


# ── Reset da empresa ──────────────────────────────────────────────────────────
def reset_company_data(cur: psycopg.Cursor, company_id: str) -> None:
    statements = [
        """DELETE FROM "SaleOrderItemAddons"
           WHERE "SaleOrderItemId" IN (
               SELECT soi."Id" FROM "SaleOrderItems" soi
               JOIN "SaleOrders" so ON so."Id" = soi."SaleOrderId"
               WHERE so."CompanyId" = %s)""",
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
        """DELETE FROM "ProductSyncItems"
           WHERE "JobId" IN (SELECT "Id" FROM "ProductSyncJobs" WHERE "CompanyId" = %s)""",
        """DELETE FROM "ProductSyncJobs" WHERE "CompanyId" = %s""",
        """DELETE FROM "ExternalProductSnapshots" WHERE "CompanyId" = %s""",
        """DELETE FROM "Products" WHERE "CompanyId" = %s""",
        """DELETE FROM "Categories" WHERE "CompanyId" = %s""",
        """DELETE FROM "Brands" WHERE "CompanyId" = %s""",
        """DELETE FROM "Customers" WHERE "CompanyId" = %s""",
    ]
    for sql in statements:
        cur.execute("SAVEPOINT sp_reset")
        try:
            cur.execute(sql, (company_id,))
            cur.execute("RELEASE SAVEPOINT sp_reset")
        except Exception:
            cur.execute("ROLLBACK TO SAVEPOINT sp_reset")
            cur.execute("RELEASE SAVEPOINT sp_reset")

    # Garante remoção de Products e Categories independente de FKs pendentes
    cur.execute("""
        DELETE FROM "OrderItems" oi
        USING "Products" p
        WHERE oi."ProductId" = p."Id" AND p."CompanyId" = %s
    """, (company_id,))
    cur.execute("""DELETE FROM "ProductVariants" WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""", (company_id,))
    cur.execute("""DELETE FROM "ProductAddons"   WHERE "ProductId" IN (SELECT "Id" FROM "Products" WHERE "CompanyId" = %s)""", (company_id,))
    cur.execute("""DELETE FROM "Products"    WHERE "CompanyId" = %s""", (company_id,))
    cur.execute("""DELETE FROM "Categories"  WHERE "CompanyId" = %s""", (company_id,))


# ── Main ──────────────────────────────────────────────────────────────────────
def run() -> None:
    now = datetime.now(timezone.utc)

    with psycopg.connect(DSN) as conn:
        with conn.cursor() as cur:
            cur.execute('SELECT "Id"::text, "Name" FROM "Companies" WHERE "Slug" = %s', (TARGET_SLUG,))
            row = cur.fetchone()
            if row is None:
                raise RuntimeError(f"Empresa com slug '{TARGET_SLUG}' não encontrada.")
            company_id, company_name = row
            print(f"→ Empresa: {company_name} ({company_id})")

            reset_company_data(cur, company_id)
            print("→ Reset concluído.")

            # Categorias (ordem do cardápio)
            category_map: dict[str, str] = {}
            all_cats = {p.category for p in CATALOG}
            unknown = all_cats - set(CATEGORY_ORDER)
            if unknown:
                raise ValueError(f"Categorias sem ordem definida: {unknown}")

            for sort_order, cat_name in enumerate(CATEGORY_ORDER, start=1):
                if cat_name not in all_cats:
                    continue
                cat_id = str(uuid4())
                category_map[cat_name] = cat_id
                cur.execute(
                    """INSERT INTO "Categories" ("Id","CompanyId","Name","Slug","SortOrder")
                       VALUES (%s,%s,%s,%s,%s)""",
                    (cat_id, company_id, cat_name, slugify(cat_name), sort_order),
                )

            # Produtos
            used_slugs: set[str] = set()
            total_variants = 0
            total_addons = 0

            for p in CATALOG:
                base_slug = slugify(p.name)
                slug = base_slug
                i = 2
                while slug in used_slugs:
                    slug = f"{base_slug}-{i}"
                    i += 1
                used_slugs.add(slug)

                product_id = str(uuid4())
                has_addons = len(p.addons) > 0

                cur.execute(
                    """INSERT INTO "Products"
                        ("Id","CompanyId","Name","Slug","CategoryId","Description",
                         "Unit","PriceCents","CostCents","MarginPercent","StockQty",
                         "IsActive","CreatedAtUtc","HasAddons","IsSupply")
                       VALUES (%s,%s,%s,%s,%s,%s,'UN',%s,0,0,0,true,%s,%s,false)""",
                    (
                        product_id, company_id, p.name, slug,
                        category_map[p.category],
                        p.description or None,
                        cents(p.price), now, has_addons,
                    ),
                )

                # Variantes (Tamanho P/G, Tipo de pão, etc.)
                for v in p.variants:
                    cur.execute(
                        """INSERT INTO "ProductVariants"
                            ("Id","ProductId","VariantKey","VariantValue","PriceCents","StockQty","IsActive")
                           VALUES (%s,%s,%s,%s,%s,0,true)""",
                        (str(uuid4()), product_id, v.key, v.value, cents(v.price)),
                    )
                    total_variants += 1

                # Adicionais (sabores R$0 + extras pagos)
                for sort_order, addon in enumerate(p.addons, start=1):
                    cur.execute(
                        """INSERT INTO "ProductAddons"
                            ("Id","ProductId","Name","PriceCents","SortOrder","IsActive","CreatedAtUtc")
                           VALUES (%s,%s,%s,%s,%s,true,%s)""",
                        (str(uuid4()), product_id, addon.name, cents(addon.price), sort_order, now),
                    )
                    total_addons += 1

            print(
                f"→ {len(category_map)} categorias | "
                f"{len(CATALOG)} produtos | "
                f"{total_variants} variantes | "
                f"{total_addons} adicionais"
            )

            if DRY_RUN:
                conn.rollback()
                print("[DRY-RUN] Rollback executado — nenhuma alteração persistida.")
                return

        conn.commit()
    print(f"✓ Importação concluída para '{TARGET_SLUG}'.")


if __name__ == "__main__":
    run()
