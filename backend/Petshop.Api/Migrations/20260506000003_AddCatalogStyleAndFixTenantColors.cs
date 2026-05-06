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

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "StoreFrontConfigs"
                DROP COLUMN IF EXISTS "CatalogStyle";
            """);
    }
}
