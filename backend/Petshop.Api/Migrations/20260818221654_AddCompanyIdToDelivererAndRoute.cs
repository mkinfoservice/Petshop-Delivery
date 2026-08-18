using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Petshop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToDelivererAndRoute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Routes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Deliverers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Backfill: infere CompanyId de cada Route a partir da empresa dos pedidos
            // (RouteStops -> Orders) que ela atende. Se uma rota tiver stops de mais
            // de uma empresa (não deveria acontecer, dado que CreateRouteAsync sempre
            // validou pedidos de uma única empresa), fica com a primeira encontrada.
            migrationBuilder.Sql("""
                UPDATE "Routes" r
                SET "CompanyId" = sub."CompanyId"
                FROM (
                    SELECT DISTINCT ON (rs."RouteId") rs."RouteId", o."CompanyId"
                    FROM "RouteStops" rs
                    JOIN "Orders" o ON o."Id" = rs."OrderId"
                    WHERE o."CompanyId" IS NOT NULL
                    ORDER BY rs."RouteId", o."CompanyId"
                ) sub
                WHERE r."Id" = sub."RouteId";
                """);

            // Rotas sem nenhum stop com pedido resolvível (ex: rota vazia) caem no
            // tenant de demo — não há cliente real em produção ainda neste ponto.
            migrationBuilder.Sql("""
                UPDATE "Routes"
                SET "CompanyId" = '11111111-0000-0000-0000-000000000001'
                WHERE "CompanyId" = '00000000-0000-0000-0000-000000000000';
                """);

            // Backfill: infere CompanyId de cada Deliverer a partir das rotas que ele já rodou.
            migrationBuilder.Sql("""
                UPDATE "Deliverers" d
                SET "CompanyId" = sub."CompanyId"
                FROM (
                    SELECT DISTINCT ON (r."DelivererId") r."DelivererId", r."CompanyId"
                    FROM "Routes" r
                    WHERE r."DelivererId" IS NOT NULL
                    ORDER BY r."DelivererId", r."CompanyId"
                ) sub
                WHERE d."Id" = sub."DelivererId";
                """);

            // Entregadores sem nenhuma rota (nunca usados) caem no tenant de demo pelo mesmo motivo.
            migrationBuilder.Sql("""
                UPDATE "Deliverers"
                SET "CompanyId" = '11111111-0000-0000-0000-000000000001'
                WHERE "CompanyId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Routes_CompanyId",
                table: "Routes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliverers_CompanyId",
                table: "Deliverers",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Routes_CompanyId",
                table: "Routes");

            migrationBuilder.DropIndex(
                name: "IX_Deliverers_CompanyId",
                table: "Deliverers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Deliverers");
        }
    }
}
