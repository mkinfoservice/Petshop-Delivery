"""
import_petshop.py
=================
Importa o catálogo completo de um Petshop varejista para uma empresa do VendApps.

USO:
    python import_petshop.py                          # petshop-demo, local
    CARDAPIO_TARGET_SLUG=meu-petshop python import_petshop.py
    CARDAPIO_DRY_RUN=1 python import_petshop.py       # simulação sem commit

VARIÁVEIS DE AMBIENTE:
    CARDAPIO_DSN          — DSN do banco (padrão: local)
    CARDAPIO_TARGET_SLUG  — slug da empresa alvo (padrão: petshop-demo)
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
DEFAULT_TARGET_SLUG = "petshop-demo"

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
    """Variante de embalagem, porte ou tipo (ex: 3 kg, Médio, Higiênica)."""
    key: str       # "Embalagem" | "Porte" | "Tipo"
    value: str     # "3 kg" | "Médio (M)" | "Higiênica"
    price: float


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


# ── Atalhos de variantes ──────────────────────────────────────────────────────
def E(*args) -> list[Variant]:
    """Variantes de Embalagem (peso): E('1 kg', 45.0, '3 kg', 110.0, '15 kg', 380.0)"""
    assert len(args) % 2 == 0
    return [Variant("Embalagem", args[i], args[i + 1]) for i in range(0, len(args), 2)]


def P(*args) -> list[Variant]:
    """Variantes de Porte (tamanho): P('Pequeno (P)', 35.0, 'Médio (M)', 45.0)"""
    assert len(args) % 2 == 0
    return [Variant("Porte", args[i], args[i + 1]) for i in range(0, len(args), 2)]


def T(*args) -> list[Variant]:
    """Variantes de Tipo de serviço: T('Higiênica', 40.0, 'Completa', 80.0)"""
    assert len(args) % 2 == 0
    return [Variant("Tipo", args[i], args[i + 1]) for i in range(0, len(args), 2)]


# ── Adicionais de serviço ─────────────────────────────────────────────────────
BANHO_ADDONS = [
    Addon("Hidratação profunda", 15.00),
    Addon("Perfume especial", 10.00),
    Addon("Limpeza de ouvido", 8.00),
    Addon("Escovação de dentes", 12.00),
    Addon("Limpeza de glândulas anais", 15.00),
    Addon("Corte de unhas", 8.00),
]


# ── Catálogo completo ─────────────────────────────────────────────────────────
CATALOG: list[ProductDef] = [

    # ═══════════════════════════════════════════════════
    # 1. Ração para Cães
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Ração para Cães", "Royal Canin Medium Adult",
        "Ração super premium para cães adultos de porte médio (11–25 kg). "
        "Fórmula balanceada para manutenção do peso ideal e saúde digestiva.",
        price=109.90,
        variants=E("2,5 kg", 109.90, "10,1 kg", 329.90, "15 kg", 459.90),
    ),
    ProductDef(
        "Ração para Cães", "Royal Canin Mini Adult",
        "Ração super premium para cães adultos de porte mini (até 10 kg). "
        "Crocância adaptada ao tamanho da boca de cães pequenos.",
        price=99.90,
        variants=E("2,5 kg", 99.90, "7,5 kg", 259.90),
    ),
    ProductDef(
        "Ração para Cães", "Royal Canin Filhotes",
        "Ração super premium para filhotes de porte médio. "
        "Suporte ao sistema imunológico e desenvolvimento ósseo.",
        price=89.90,
        variants=E("1 kg", 89.90, "3 kg", 199.90),
    ),
    ProductDef(
        "Ração para Cães", "Pedigree Adulto Carne e Frango",
        "Ração completa para cães adultos de todas as raças. "
        "Formulada com vitaminas e minerais essenciais para a saúde e vitalidade.",
        price=32.90,
        variants=E("1 kg", 32.90, "3 kg", 79.90, "15 kg", 269.90),
    ),
    ProductDef(
        "Ração para Cães", "Golden Fórmula Adulto",
        "Ração premium para cães adultos. "
        "Com frango, arroz e cereais, desenvolvida por nutricionistas veterinários.",
        price=49.90,
        variants=E("1 kg", 49.90, "3 kg", 119.90, "15 kg", 389.90),
    ),
    ProductDef(
        "Ração para Cães", "Premier Nutrição Clínica Adulto",
        "Ração super premium para cães adultos de porte médio. "
        "Alta digestibilidade e palatabilidade, com prebióticos para saúde intestinal.",
        price=119.90,
        variants=E("2,5 kg", 119.90, "10,1 kg", 369.90),
    ),
    ProductDef(
        "Ração para Cães", "Guabi Natural Cão Adulto",
        "Ração natural para cães adultos. Sem corantes artificiais, "
        "com ingredientes de origem natural e alto teor de proteína.",
        price=89.90,
        variants=E("3 kg", 89.90, "10,1 kg", 229.90),
    ),
    ProductDef(
        "Ração para Cães", "N&D Grain Free Frango",
        "Ração sem grãos para cães adultos de porte médio. "
        "Alto teor proteico com frango e romã, sem glúten.",
        price=159.90,
        variants=E("2,5 kg", 159.90, "10 kg", 479.90),
    ),

    # ═══════════════════════════════════════════════════
    # 2. Ração para Gatos
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Ração para Gatos", "Royal Canin Fit 32",
        "Ração super premium para gatos adultos que vivem ao ar livre ou em interior. "
        "Apoia o sistema imunológico e a saúde urinária.",
        price=94.90,
        variants=E("1,5 kg", 94.90, "7,5 kg", 349.90),
    ),
    ProductDef(
        "Ração para Gatos", "Royal Canin Indoor",
        "Ração super premium para gatos adultos que vivem em apartamento. "
        "Controle de pelos e odores, com fibras específicas.",
        price=99.90,
        variants=E("1,5 kg", 99.90, "7,5 kg", 369.90),
    ),
    ProductDef(
        "Ração para Gatos", "Whiskas Adulto Carne",
        "Ração completa para gatos adultos com sabor de carne. "
        "Vitaminas e minerais para saúde plena, palatabilidade comprovada.",
        price=27.90,
        variants=E("500 g", 27.90, "1 kg", 49.90, "3 kg", 119.90),
    ),
    ProductDef(
        "Ração para Gatos", "Purina One Adulto Frango",
        "Ração premium para gatos adultos com frango como 1º ingrediente. "
        "Suporte à saúde renal e sistema urinário.",
        price=69.90,
        variants=E("1,5 kg", 69.90, "3 kg", 119.90),
    ),
    ProductDef(
        "Ração para Gatos", "Premier Gatos Adulto Castrados",
        "Ração super premium formulada especialmente para gatos castrados. "
        "Controle de peso e saúde urinária.",
        price=109.90,
        variants=E("1,5 kg", 109.90, "7,5 kg", 399.90),
    ),
    ProductDef(
        "Ração para Gatos", "N&D Grain Free Gato",
        "Ração sem grãos para gatos adultos. "
        "Alta proteína com pato e uva, sem glúten e sem corantes artificiais.",
        price=149.90,
        variants=E("1,5 kg", 149.90, "5 kg", 399.90),
    ),

    # ═══════════════════════════════════════════════════
    # 3. Petiscos e Snacks
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Petiscos e Snacks", "Bifinho Frango para Cães",
        "Petisco macio e saboroso de frango para cães. "
        "Rico em proteínas, sem corantes artificiais. Ideal para recompensas e adestramento.",
        price=12.90,
        variants=E("65 g", 12.90, "250 g", 39.90),
    ),
    ProductDef(
        "Petiscos e Snacks", "Ossinho Defumado",
        "Ossinho natural defumado para cães. "
        "Auxilia na higiene bucal e entretém por horas. Consulte os tamanhos disponíveis.",
        price=8.90,
        variants=P("Pequeno (P)", 8.90, "Médio (M)", 14.90, "Grande (G)", 22.90),
    ),
    ProductDef(
        "Petiscos e Snacks", "Snack Dental Clean",
        "Petisco funcional para higiene bucal de cães. "
        "Reduz o acúmulo de tártaro e o mau hálito. Formato espiral de ação mecânica.",
        price=19.90,
    ),
    ProductDef(
        "Petiscos e Snacks", "Petisco Catnip para Gatos",
        "Petisco com catnip (erva-gateira) para gatos. "
        "Estimula brincadeiras e reduz o estresse. 100% natural.",
        price=9.90,
    ),
    ProductDef(
        "Petiscos e Snacks", "Temptations Mix de Sabores Gato",
        "Petisco crocante por fora e macio por dentro para gatos. "
        "Mix de frango, carne e salmão. Favorito dos gatos.",
        price=14.90,
    ),
    ProductDef(
        "Petiscos e Snacks", "Jerky de Frango Desidratado",
        "Petisco natural de frango desidratado para cães. "
        "Ingrediente único, sem conservantes, aditivos ou corantes.",
        price=24.90,
    ),

    # ═══════════════════════════════════════════════════
    # 4. Medicamentos e Saúde
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Medicamentos e Saúde", "Antipulgas Frontline Plus Cão",
        "Antipulgas e carrapaticida para cães. Proteção por até 30 dias. "
        "Aplicação tópica (spot-on). Consulte o porte indicado.",
        price=59.90,
        variants=P(
            "Até 10 kg", 59.90,
            "10 a 20 kg", 64.90,
            "20 a 40 kg", 69.90,
        ),
    ),
    ProductDef(
        "Medicamentos e Saúde", "Antipulgas Frontline Plus Gato",
        "Antipulgas e carrapaticida para gatos. Proteção por até 30 dias. "
        "Aplicação tópica (spot-on). Para gatos acima de 1 kg.",
        price=59.90,
    ),
    ProductDef(
        "Medicamentos e Saúde", "Vermífugo Drontal Cão",
        "Vermífugo de amplo espectro para cães. "
        "Combate áscaris, ancilóstomos, tênias e outros parasitas intestinais.",
        price=39.90,
        variants=E("2 comprimidos", 39.90, "4 comprimidos", 69.90),
    ),
    ProductDef(
        "Medicamentos e Saúde", "Vermífugo Drontal Gato",
        "Vermífugo de amplo espectro para gatos. "
        "Combate parasitas intestinais. Dose conforme o peso do animal.",
        price=42.90,
    ),
    ProductDef(
        "Medicamentos e Saúde", "Suplemento Articular Condroitina",
        "Suplemento para saúde articular de cães e gatos. "
        "Combinação de condroitina, glucosamina e colágeno. Indicado para animais idosos.",
        price=54.90,
    ),
    ProductDef(
        "Medicamentos e Saúde", "Suplemento Pelo e Pele Omega 3",
        "Suplemento com ômega 3, 6 e 9 para pelagem e pele saudáveis. "
        "Reduz queda de pelos e coceira. Para cães e gatos.",
        price=44.90,
    ),
    ProductDef(
        "Medicamentos e Saúde", "Probiótico Intestinal Pet",
        "Probiótico para equilíbrio da flora intestinal de cães e gatos. "
        "Auxilia em casos de diarreia, vômito e transições de ração.",
        price=49.90,
    ),

    # ═══════════════════════════════════════════════════
    # 5. Higiene e Banho
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Higiene e Banho", "Shampoo Neutro Cão",
        "Shampoo neutro de uso frequente para cães. "
        "pH balanceado, sem parabenos. Não agride a pele nem a pelagem.",
        price=19.90,
        variants=E("500 ml", 19.90, "1 L", 34.90),
    ),
    ProductDef(
        "Higiene e Banho", "Shampoo Antipulgas Cão",
        "Shampoo com ação repelente de pulgas e carrapatos para cães. "
        "Com extrato de neem e aloe vera. Uso no momento do banho.",
        price=24.90,
    ),
    ProductDef(
        "Higiene e Banho", "Shampoo Neutro Gato",
        "Shampoo específico para gatos. pH neutro, fórmula suave "
        "que não resseca a pele. Indicado para uso periódico.",
        price=22.90,
    ),
    ProductDef(
        "Higiene e Banho", "Condicionador Pet Brilho e Maciez",
        "Condicionador hidratante para pelos de cães e gatos. "
        "Facilita a escovação, reduz o frizz e deixa o pelo brilhante.",
        price=21.90,
        variants=E("500 ml", 21.90, "1 L", 36.90),
    ),
    ProductDef(
        "Higiene e Banho", "Perfume Pet Cotton Fresh",
        "Perfume para pets com fragrância algodão. "
        "Sem álcool, hipoalergênico. Durabilidade de até 3 dias.",
        price=19.90,
    ),
    ProductDef(
        "Higiene e Banho", "Escova de Dentes Pet Kit",
        "Kit escova + pasta dental enzimática para cães. "
        "Sabor frango. Previne tártaro e mau hálito. Não contém flúor.",
        price=29.90,
    ),
    ProductDef(
        "Higiene e Banho", "Toalha Microfibra Pet",
        "Toalha de microfibra superabsorvente para cães e gatos. "
        "Seca 3x mais rápido que toalha comum. Lavável e durável.",
        price=34.90,
        variants=P("Pequena (P)", 34.90, "Média (M)", 44.90, "Grande (G)", 59.90),
    ),
    ProductDef(
        "Higiene e Banho", "Luva de Remoção de Pelos",
        "Luva de borracha para remoção de pelos de pets em sofás, camas e tapetes. "
        "Também usada para escovação rápida do animal.",
        price=24.90,
    ),

    # ═══════════════════════════════════════════════════
    # 6. Coleiras e Guias
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Coleiras e Guias", "Coleira Nylon Ajustável",
        "Coleira de nylon resistente com fivela de segurança. "
        "Lavável, disponível em diversas cores. Para uso diário.",
        price=29.90,
        variants=P("Pequeno (P)", 29.90, "Médio (M)", 39.90, "Grande (G)", 49.90),
    ),
    ProductDef(
        "Coleiras e Guias", "Coleira Couro Premium",
        "Coleira de couro legítimo com acabamento costurado à mão. "
        "Durável, elegante. Disponível com enfeites personalizados.",
        price=79.90,
        variants=P("Pequeno (P)", 79.90, "Médio (M)", 99.90, "Grande (G)", 119.90),
    ),
    ProductDef(
        "Coleiras e Guias", "Guia Simples 1,5 m",
        "Guia básica de nylon para passeios. Comprimento 1,5 m. "
        "Resistente e leve, com mosquetão giratório.",
        price=24.90,
        variants=P("Pequeno (P)", 24.90, "Médio (M)", 34.90, "Grande (G)", 44.90),
    ),
    ProductDef(
        "Coleiras e Guias", "Guia Retrátil 5 m",
        "Guia retrátil com trava automática. Comprimento de até 5 m. "
        "Para cães de até 50 kg. Ergonômica e confortável.",
        price=89.90,
        variants=P("Até 20 kg", 89.90, "Até 50 kg", 119.90),
    ),
    ProductDef(
        "Coleiras e Guias", "Peitoral Acolchoado Anti-puxão",
        "Peitoral de malha acolchoada que distribui a pressão uniformemente. "
        "Reduz puxões sem machucar o pescoço. Sistema anti-escape.",
        price=69.90,
        variants=P("Pequeno (P)", 69.90, "Médio (M)", 89.90, "Grande (G)", 109.90),
    ),

    # ═══════════════════════════════════════════════════
    # 7. Comedouros e Bebedouros
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Comedouros e Bebedouros", "Comedouro Inox Antiderrapante",
        "Comedouro de aço inox com base antiderrapante de borracha. "
        "Higiênico, durável e fácil de lavar. Para cães e gatos.",
        price=29.90,
        variants=P("Pequeno (P)", 29.90, "Médio (M)", 39.90, "Grande (G)", 54.90),
    ),
    ProductDef(
        "Comedouros e Bebedouros", "Bebedouro Fonte Elétrica Pet",
        "Fonte de água com filtro para pets. "
        "Mantém a água fresca e circulante, estimulando o consumo de água. "
        "Indicado especialmente para gatos.",
        price=149.90,
    ),
    ProductDef(
        "Comedouros e Bebedouros", "Comedouro Elevado Duplo",
        "Comedouro duplo com suporte elevado. Ideal para raças grandes e cães idosos. "
        "Facilita a digestão e reduz problemas de coluna.",
        price=89.90,
        variants=P("Médio (M)", 89.90, "Grande (G)", 119.90),
    ),
    ProductDef(
        "Comedouros e Bebedouros", "Dispenser Automático de Água",
        "Bebedouro gravidade para cães e gatos. "
        "Reservatório de 2L. Alimentação automática conforme o consumo.",
        price=49.90,
    ),
    ProductDef(
        "Comedouros e Bebedouros", "Comedouro Lento Anti-Engolimento",
        "Comedouro com labirinto interno que reduz a velocidade de ingestão. "
        "Previne engasgo e síndrome de dilatação gástrica.",
        price=39.90,
        variants=P("Pequeno (P)", 39.90, "Médio (M)", 49.90),
    ),

    # ═══════════════════════════════════════════════════
    # 8. Brinquedos
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Brinquedos", "Corda de Algodão Trançada",
        "Brinquedo de corda de algodão para cães. "
        "Ótimo para cabo de guerra e higiene dental durante a brincadeira.",
        price=19.90,
        variants=P("Pequeno (P)", 19.90, "Médio (M)", 29.90, "Grande (G)", 39.90),
    ),
    ProductDef(
        "Brinquedos", "Bola com Apito",
        "Bola de borracha resistente com apito interno para cães. "
        "Estimula instinto de caça, resistente a mordidas.",
        price=14.90,
        variants=P("Pequeno (P)", 14.90, "Médio (M)", 22.90, "Grande (G)", 32.90),
    ),
    ProductDef(
        "Brinquedos", "Kong Recheável Borracha",
        "Brinquedo interativo de borracha para cães. "
        "Pode ser recheado com ração ou petisco. "
        "Estimula o raciocínio e alivia ansiedade.",
        price=59.90,
        variants=P("Pequeno (P)", 59.90, "Médio (M)", 79.90, "Grande (G)", 99.90),
    ),
    ProductDef(
        "Brinquedos", "Arranhador Gato Torre",
        "Torre arranhador para gatos com plataformas e corda sisal. "
        "Estimula arranhar e escalar. Protege seus móveis.",
        price=129.90,
    ),
    ProductDef(
        "Brinquedos", "Ratinho de Pelúcia com Catnip",
        "Brinquedo ratinho de pelúcia recheado com catnip para gatos. "
        "Estimula brincadeiras e enriquecimento ambiental.",
        price=12.90,
    ),
    ProductDef(
        "Brinquedos", "Varinha Interativa para Gatos",
        "Varinha com penas e brilhos para brincadeiras interativas com gatos. "
        "Estimula o instinto de caça e exercício físico.",
        price=24.90,
    ),
    ProductDef(
        "Brinquedos", "Brinquedo Puzzle Inteligente",
        "Brinquedo interativo nível 2 para cães e gatos. "
        "Esconde petiscos em compartimentos que o animal deve descobrir. "
        "Estimula cognição e reduz tédio.",
        price=69.90,
    ),

    # ═══════════════════════════════════════════════════
    # 9. Camas e Abrigos
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Camas e Abrigos", "Cama Pet Pelúcia",
        "Cama redonda de pelúcia macia para cães e gatos. "
        "Bordas elevadas para apoio da cabeça. Lavável na máquina.",
        price=79.90,
        variants=P("Pequeno (P)", 79.90, "Médio (M)", 109.90, "Grande (G)", 149.90),
    ),
    ProductDef(
        "Camas e Abrigos", "Cama Retangular com Zíper",
        "Cama retangular com capa removível e lavável. "
        "Enchimento em flocos de espuma. Ideal para cães que não gostam de bordas.",
        price=89.90,
        variants=P("Pequeno (P)", 89.90, "Médio (M)", 119.90, "Grande (G)", 169.90),
    ),
    ProductDef(
        "Camas e Abrigos", "Casinha de Madeira MDF",
        "Casinha em MDF resistente e tratado contra umidade. "
        "Telhado removível para limpeza fácil. Para uso interno ou externo.",
        price=199.90,
        variants=P("Pequeno (P)", 199.90, "Médio (M)", 289.90, "Grande (G)", 399.90),
    ),
    ProductDef(
        "Camas e Abrigos", "Manta Soft Pet",
        "Manta de soft macia e quentinha para pets. "
        "Leve, lavável e antialérgica. Ideal para camas, sofás e carrinhos.",
        price=49.90,
        variants=P("Pequeno (P)", 49.90, "Médio (M)", 69.90, "Grande (G)", 89.90),
    ),
    ProductDef(
        "Camas e Abrigos", "Iglu Pet Interior",
        "Cama iglu com entrada frontal para cães e gatos pequenos. "
        "Pelúcia interna super macia. Sensação de aconchego e segurança.",
        price=99.90,
        variants=P("Pequeno (P)", 99.90, "Médio (M)", 139.90),
    ),

    # ═══════════════════════════════════════════════════
    # 10. Aquários e Peixes
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Aquários e Peixes", "Aquário Starter Kit 20 L",
        "Kit completo com aquário de vidro 20 litros, filtro interno, "
        "termômetro e tampa com iluminação LED. Ideal para iniciantes.",
        price=189.90,
    ),
    ProductDef(
        "Aquários e Peixes", "Aquário Kit 60 L",
        "Aquário de vidro 60 litros com filtro externo, aquecedor e "
        "iluminação LED. Para peixes tropicais e comunidade.",
        price=399.90,
    ),
    ProductDef(
        "Aquários e Peixes", "Ração Tropical Flake",
        "Ração em flocos para peixes tropicais de água doce. "
        "Fórmula enriquecida com vitaminas e espirulina. Realça as cores.",
        price=19.90,
        variants=E("50 g", 19.90, "200 g", 59.90),
    ),
    ProductDef(
        "Aquários e Peixes", "Filtro Interno para Aquário",
        "Filtro interno com esponja e carvão ativado. "
        "Para aquários de até 60 litros. Vazão regulável.",
        price=49.90,
    ),
    ProductDef(
        "Aquários e Peixes", "Aquecedor Automático 100W",
        "Aquecedor com termostato automático para aquários tropicais. "
        "Mantém temperatura estável entre 20°C e 32°C. Para até 100 litros.",
        price=79.90,
    ),

    # ═══════════════════════════════════════════════════
    # 11. Aves e Roedores
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Aves e Roedores", "Ração para Calopsita e Periquito",
        "Mistura de sementes selecionadas para calopsitas e periquitos. "
        "Com frutas desidratadas, vitaminas e minerais. Sem conservantes.",
        price=24.90,
        variants=E("500 g", 24.90, "1 kg", 42.90),
    ),
    ProductDef(
        "Aves e Roedores", "Ração para Hamster e Gerbil",
        "Ração completa para hamsters, gerbils e outros pequenos roedores. "
        "Mix de grãos, sementes e pellets. Rica em fibras.",
        price=19.90,
        variants=E("400 g", 19.90, "800 g", 34.90),
    ),
    ProductDef(
        "Aves e Roedores", "Ração para Coelho e Porquinho-da-índia",
        "Ração em pellets para coelhos e porquinhos-da-índia. "
        "Alta em fibras vegetais, com vitamina C adicionada.",
        price=29.90,
        variants=E("500 g", 29.90, "2 kg", 89.90),
    ),
    ProductDef(
        "Aves e Roedores", "Brinquedo Roda de Exercício para Hamster",
        "Roda de exercício silenciosa para hamsters e gerbils. "
        "Superfície antiderrapante, sem raios que possam machucar.",
        price=34.90,
    ),
    ProductDef(
        "Aves e Roedores", "Poleiro e Balanço para Pássaros",
        "Kit com poleiro de madeira natural e balanço para aves de pequeno porte. "
        "Estimula o exercício e o entretenimento na gaiola.",
        price=19.90,
    ),

    # ═══════════════════════════════════════════════════
    # 12. Serviços Pet
    # ═══════════════════════════════════════════════════
    ProductDef(
        "Serviços Pet", "Banho Completo",
        "Banho completo com shampoo e condicionador específicos para o tipo de pelo. "
        "Inclui secagem, escovação, limpeza de ouvidos e corte de unhas.",
        price=60.00,
        variants=P(
            "Pequeno (até 5 kg)", 60.00,
            "Médio (5 a 15 kg)", 85.00,
            "Grande (acima de 15 kg)", 120.00,
        ),
        addons=BANHO_ADDONS,
    ),
    ProductDef(
        "Serviços Pet", "Banho e Tosa Higiênica",
        "Banho completo + tosa higiênica (barriga, virilha, patas e focinho). "
        "Ideal para manutenção entre as tosas completas.",
        price=80.00,
        variants=P(
            "Pequeno (até 5 kg)", 80.00,
            "Médio (5 a 15 kg)", 110.00,
            "Grande (acima de 15 kg)", 150.00,
        ),
        addons=BANHO_ADDONS,
    ),
    ProductDef(
        "Serviços Pet", "Banho e Tosa Completa",
        "Banho completo + tosa no padrão da raça ou conforme solicitação do tutor. "
        "Pelagem aparada, modelada e finalizada.",
        price=120.00,
        variants=P(
            "Pequeno (até 5 kg)", 120.00,
            "Médio (5 a 15 kg)", 160.00,
            "Grande (acima de 15 kg)", 220.00,
        ),
        addons=BANHO_ADDONS,
    ),
    ProductDef(
        "Serviços Pet", "Tosa na Tesoura",
        "Tosa artística realizada exclusivamente na tesoura, sem máquina. "
        "Indicada para raças com pelos finos e delicados. Finalização premium.",
        price=150.00,
        variants=P(
            "Pequeno (até 5 kg)", 150.00,
            "Médio (5 a 15 kg)", 200.00,
        ),
        addons=BANHO_ADDONS,
    ),
    ProductDef(
        "Serviços Pet", "Corte de Unhas Avulso",
        "Corte e lixamento das unhas do pet. "
        "Serviço rápido sem necessidade de banho. Cães e gatos.",
        price=25.00,
    ),
    ProductDef(
        "Serviços Pet", "Consulta Veterinária",
        "Consulta clínica geral com médico veterinário. "
        "Avaliação completa, orientações de saúde preventiva e emissão de receituário.",
        price=150.00,
    ),
]

# Ordem de exibição das categorias no catálogo
CATEGORY_ORDER = [
    "Ração para Cães",
    "Ração para Gatos",
    "Petiscos e Snacks",
    "Medicamentos e Saúde",
    "Higiene e Banho",
    "Coleiras e Guias",
    "Comedouros e Bebedouros",
    "Brinquedos",
    "Camas e Abrigos",
    "Aquários e Peixes",
    "Aves e Roedores",
    "Serviços Pet",
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

            # Categorias (ordem do catálogo)
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

                # Variantes (Embalagem, Porte, Tipo de serviço, etc.)
                for v in p.variants:
                    cur.execute(
                        """INSERT INTO "ProductVariants"
                            ("Id","ProductId","VariantKey","VariantValue","PriceCents","StockQty","IsActive")
                           VALUES (%s,%s,%s,%s,%s,0,true)""",
                        (str(uuid4()), product_id, v.key, v.value, cents(v.price)),
                    )
                    total_variants += 1

                # Adicionais (serviços extras)
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
