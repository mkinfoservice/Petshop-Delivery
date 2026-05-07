using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260506000005_SetGoCoffeeBranding")]
[DbContext(typeof(AppDbContext))]
public class SetGoCoffeeBranding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs" sf
            SET "PrimaryColor"   = '#C8953A',
                "SecondaryColor" = '#1C1209',
                "AccentColor"    = '#A07230',
                "BgColor"        = '#FAF7F2',
                "Surface2Color"  = '#F5EDE0',
                "BorderColor"    = 'rgba(107,79,58,0.13)',
                "TextColor"      = '#1C1209',
                "TextMutedColor" = '#6B4F3A',
                "CatalogStyle"   = 'coffee'
            FROM "Companies" c
            WHERE sf."CompanyId" = c."Id"
              AND c."Slug" = 'novaempresa';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
