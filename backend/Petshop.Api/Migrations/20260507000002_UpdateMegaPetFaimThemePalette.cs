using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260507000002_UpdateMegaPetFaimThemePalette")]
[DbContext(typeof(AppDbContext))]
public class UpdateMegaPetFaimThemePalette : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs" sf
            SET "LogoUrl"        = '/tenant-assets/mega-pet-faim-logo.svg',
                "StoreName"      = 'Mega Pet Faim',
                "PrimaryColor"   = '#FF5722',
                "SecondaryColor" = '#1E3A8A',
                "AccentColor"    = '#FFA726',
                "BgColor"        = '#F8F9FA',
                "Surface2Color"  = '#EEF2F7',
                "BorderColor"    = '#DDE3EE',
                "TextColor"      = '#0B1220',
                "TextMutedColor" = '#475569'
            FROM "Companies" c
            WHERE sf."CompanyId" = c."Id"
              AND c."Slug" = 'megapetfaim';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
