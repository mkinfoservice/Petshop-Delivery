using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260504000002_AddStoreFrontThemeColors")]
[DbContext(typeof(AppDbContext))]
public class AddStoreFrontThemeColors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Adiciona colunas de tema com defaults neutros (branco/cinza)
        migrationBuilder.Sql("""
            ALTER TABLE "StoreFrontConfigs"
                ADD COLUMN IF NOT EXISTS "BgColor"        varchar(50) NOT NULL DEFAULT '#ffffff',
                ADD COLUMN IF NOT EXISTS "Surface2Color"  varchar(50) NOT NULL DEFAULT '#f3f4f6',
                ADD COLUMN IF NOT EXISTS "BorderColor"    varchar(50) NOT NULL DEFAULT 'rgba(0,0,0,0.08)',
                ADD COLUMN IF NOT EXISTS "TextColor"      varchar(20) NOT NULL DEFAULT '#111827',
                ADD COLUMN IF NOT EXISTS "TextMutedColor" varchar(20) NOT NULL DEFAULT '#6b7280';
            """);

        // Preserva a paleta quente da Go Coffee na linha dela
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs"
            SET "BgColor"        = '#FAF7F2',
                "Surface2Color"  = '#F5EDE0',
                "BorderColor"    = 'rgba(107,79,58,0.13)',
                "TextColor"      = '#1C1209',
                "TextMutedColor" = '#6B4F3A'
            WHERE "CompanyId" = '33333333-0000-0000-0000-000000000003';
            """);

        // Atualiza a cor primária hardcoded da GoCoffee (era #7c5cf8, deve ser o café)
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs"
            SET "PrimaryColor" = '#C8953A'
            WHERE "CompanyId" = '33333333-0000-0000-0000-000000000003'
              AND "PrimaryColor" = '#7c5cf8';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "StoreFrontConfigs"
                DROP COLUMN IF EXISTS "BgColor",
                DROP COLUMN IF EXISTS "Surface2Color",
                DROP COLUMN IF EXISTS "BorderColor",
                DROP COLUMN IF EXISTS "TextColor",
                DROP COLUMN IF EXISTS "TextMutedColor";
            """);
    }
}
