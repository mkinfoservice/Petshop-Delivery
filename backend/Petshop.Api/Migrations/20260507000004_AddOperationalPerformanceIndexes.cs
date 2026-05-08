using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260507000004_AddOperationalPerformanceIndexes")]
[DbContext(typeof(AppDbContext))]
public class AddOperationalPerformanceIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE INDEX IF NOT EXISTS "IX_Orders_CompanyId_CreatedAtUtc"
                ON "Orders" ("CompanyId", "CreatedAtUtc");

            CREATE INDEX IF NOT EXISTS "IX_Orders_CompanyId_Status_IsTableOrder_CreatedAtUtc"
                ON "Orders" ("CompanyId", "Status", "IsTableOrder", "CreatedAtUtc");

            CREATE INDEX IF NOT EXISTS "IX_SalesQuotes_CompanyId_IsArchived_Status_CreatedAtUtc"
                ON "SalesQuotes" ("CompanyId", "IsArchived", "Status", "CreatedAtUtc");

            CREATE INDEX IF NOT EXISTS "IX_SalesQuotes_CompanyId_Origin_CreatedAtUtc"
                ON "SalesQuotes" ("CompanyId", "Origin", "CreatedAtUtc");

            CREATE INDEX IF NOT EXISTS "IX_SalesQuotes_CompanyId_ArchivedAtUtc"
                ON "SalesQuotes" ("CompanyId", "ArchivedAtUtc")
                WHERE "IsArchived" = true AND "ArchivedAtUtc" IS NOT NULL;

            CREATE INDEX IF NOT EXISTS "IX_Routes_Status_CreatedAtUtc"
                ON "Routes" ("Status", "CreatedAtUtc");

            CREATE INDEX IF NOT EXISTS "IX_RouteStops_RouteId_Sequence"
                ON "RouteStops" ("RouteId", "Sequence");

            CREATE INDEX IF NOT EXISTS "IX_OperationalAuditLogs_CompanyId_Action_CreatedAtUtc"
                ON "OperationalAuditLogs" ("CompanyId", "Action", "CreatedAtUtc");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_OperationalAuditLogs_CompanyId_Action_CreatedAtUtc";
            DROP INDEX IF EXISTS "IX_RouteStops_RouteId_Sequence";
            DROP INDEX IF EXISTS "IX_Routes_Status_CreatedAtUtc";
            DROP INDEX IF EXISTS "IX_SalesQuotes_CompanyId_ArchivedAtUtc";
            DROP INDEX IF EXISTS "IX_SalesQuotes_CompanyId_Origin_CreatedAtUtc";
            DROP INDEX IF EXISTS "IX_SalesQuotes_CompanyId_IsArchived_Status_CreatedAtUtc";
            DROP INDEX IF EXISTS "IX_Orders_CompanyId_Status_IsTableOrder_CreatedAtUtc";
            DROP INDEX IF EXISTS "IX_Orders_CompanyId_CreatedAtUtc";
            """);
    }
}
