using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Petshop.Api.Data;

namespace Petshop.Api.Migrations;

[Migration("20260506000002_SetMegapetfaimAnnouncement")]
[DbContext(typeof(AppDbContext))]
public class SetMegapetfaimAnnouncement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Seta o announcement da megapetfaim para o texto de delivery/retirada.
        // O frontend usa announcements[0] como barra de topo — só visível quando
        // bgColor != '#ffffff' (sinal de tenant com tema configurado).
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs" sf
            SET "AnnouncementsJson" = '["Delivery e retirada disponíveis · Consulte condições de frete"]'
            FROM "Companies" c
            WHERE sf."CompanyId" = c."Id"
              AND c."Slug" = 'megapetfaim';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "StoreFrontConfigs" sf
            SET "AnnouncementsJson" = '["Frete Grátis acima de R$ 100"]'
            FROM "Companies" c
            WHERE sf."CompanyId" = c."Id"
              AND c."Slug" = 'megapetfaim';
            """);
    }
}
