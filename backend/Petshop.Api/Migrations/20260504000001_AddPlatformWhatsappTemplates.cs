using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260504000001_AddPlatformWhatsappTemplates")]
[DbContext(typeof(AppDbContext))]
public class AddPlatformWhatsappTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "PlatformWhatsappConfigs"
                ADD COLUMN IF NOT EXISTS "NotifyOnStatusesJson"      text NULL,
                ADD COLUMN IF NOT EXISTS "NotificationTemplatesJson" text NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE "PlatformWhatsappConfigs"
                DROP COLUMN IF EXISTS "NotifyOnStatusesJson",
                DROP COLUMN IF EXISTS "NotificationTemplatesJson";
            """);
    }
}
