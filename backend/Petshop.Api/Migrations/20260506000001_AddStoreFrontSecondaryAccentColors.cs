using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260506000001_AddStoreFrontSecondaryAccentColors")]
[DbContext(typeof(AppDbContext))]
public class AddStoreFrontSecondaryAccentColors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Adiciona SecondaryColor e AccentColor com defaults iguais ao primary (neutro)
        migrationBuilder.Sql("""
            ALTER TABLE "StoreFrontConfigs"
                ADD COLUMN IF NOT EXISTS "SecondaryColor" varchar(20) NOT NULL DEFAULT '#6366f1',
                ADD COLUMN IF NOT EXISTS "AccentColor"    varchar(20) NOT NULL DEFAULT '#f59e0b';
            """);

        // MegaPet Faim — paleta da logo: laranja, marinho, amarelo dourado
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs" sf
            SET "PrimaryColor"   = '#E84213',
                "SecondaryColor" = '#1B3A8C',
                "AccentColor"    = '#F5B000',
                "BgColor"        = '#FFFAF6',
                "Surface2Color"  = '#FFF3EC',
                "BorderColor"    = 'rgba(232,66,19,0.10)',
                "TextColor"      = '#1A1220',
                "TextMutedColor" = '#6B5C54'
            FROM "Companies" c
            WHERE sf."CompanyId" = c."Id"
              AND c."Slug" = 'megapetfaim';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "StoreFrontConfigs"
                DROP COLUMN IF EXISTS "SecondaryColor",
                DROP COLUMN IF EXISTS "AccentColor";
            """);
    }
}
