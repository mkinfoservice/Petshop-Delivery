using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260507000001_SetMegaPetFaimBranding")]
[DbContext(typeof(AppDbContext))]
public class SetMegaPetFaimBranding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs" sf
            SET "LogoUrl"        = '/tenant-assets/mega-pet-faim-logo.svg',
                "StoreName"      = 'Mega Pet Faim',
                "PrimaryColor"   = '#f04a24',
                "SecondaryColor" = '#123f8c',
                "AccentColor"    = '#123f8c',
                "BgColor"        = '#fff8f3',
                "Surface2Color"  = '#fff0e8',
                "BorderColor"    = 'rgba(240,74,36,0.18)',
                "TextColor"      = '#07122f',
                "TextMutedColor" = '#5f6575'
            FROM "Companies" c
            WHERE sf."CompanyId" = c."Id"
              AND c."Slug" = 'megapetfaim';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
