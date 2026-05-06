using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260506000004_RevertCrossTenantBrandingChanges")]
[DbContext(typeof(AppDbContext))]
public class RevertCrossTenantBrandingChanges : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs" sf
            SET "SecondaryColor" = '#6366f1'
            FROM "Companies" c
            WHERE sf."CompanyId" = c."Id"
              AND c."Slug" = 'novaempresa'
              AND sf."SecondaryColor" = '#1C1209';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
