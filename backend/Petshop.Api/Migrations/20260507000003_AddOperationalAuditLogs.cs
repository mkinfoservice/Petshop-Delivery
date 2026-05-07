using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260507000003_AddOperationalAuditLogs")]
[DbContext(typeof(AppDbContext))]
public class AddOperationalAuditLogs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "OperationalAuditLogs" (
                "Id" uuid NOT NULL,
                "CompanyId" uuid,
                "CompanySlug" character varying(120),
                "ActorId" character varying(100),
                "ActorUsername" character varying(120) NOT NULL,
                "ActorRole" character varying(40) NOT NULL,
                "Action" character varying(100) NOT NULL,
                "TargetType" character varying(80) NOT NULL,
                "TargetId" character varying(120) NOT NULL,
                "TargetName" character varying(200),
                "PayloadJson" text,
                "IpAddress" character varying(45),
                "UserAgent" character varying(300),
                "CorrelationId" character varying(100),
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_OperationalAuditLogs" PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS "IX_OperationalAuditLogs_CompanyId_CreatedAtUtc"
                ON "OperationalAuditLogs" ("CompanyId", "CreatedAtUtc");

            CREATE INDEX IF NOT EXISTS "IX_OperationalAuditLogs_Action_CreatedAtUtc"
                ON "OperationalAuditLogs" ("Action", "CreatedAtUtc");

            CREATE INDEX IF NOT EXISTS "IX_OperationalAuditLogs_TargetType_TargetId"
                ON "OperationalAuditLogs" ("TargetType", "TargetId");

            CREATE INDEX IF NOT EXISTS "IX_OperationalAuditLogs_CorrelationId"
                ON "OperationalAuditLogs" ("CorrelationId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP TABLE IF EXISTS "OperationalAuditLogs";""");
    }
}
