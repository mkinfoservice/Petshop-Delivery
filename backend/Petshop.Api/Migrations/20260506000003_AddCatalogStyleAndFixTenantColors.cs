using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260506000003_AddCatalogStyleAndFixTenantColors")]
[DbContext(typeof(AppDbContext))]
public class AddCatalogStyleAndFixTenantColors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Adiciona campo de estilo de catálogo: 'default' para todos, exceto tenants configurados
        migrationBuilder.Sql("""
            ALTER TABLE "StoreFrontConfigs"
                ADD COLUMN IF NOT EXISTS "CatalogStyle" varchar(30) NOT NULL DEFAULT 'default';
            """);

        // Megapetfaim usa tema petshop (header escuro + padrão de patas no catálogo moderno)
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs" sf
            SET "CatalogStyle" = 'petshop'
            FROM "Companies" c
            WHERE sf."CompanyId" = c."Id"
              AND c."Slug" = 'megapetfaim';
            """);

        // GoCoffee: SecondaryColor = espresso escuro (para admin panel por tenant)
        // O admin header usa var(--brand-2), que agora reflete a cor da marca de cada tenant
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs" sf
            SET "SecondaryColor" = '#1C1209'
            FROM "Companies" c
            WHERE sf."CompanyId" = c."Id"
              AND c."Slug" = 'novaempresa';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "StoreFrontConfigs"
                DROP COLUMN IF EXISTS "CatalogStyle";
            """);

        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs" sf
            SET "SecondaryColor" = '#6366f1'
            FROM "Companies" c
            WHERE sf."CompanyId" = c."Id"
              AND c."Slug" = 'novaempresa';
            """);
    }
}
