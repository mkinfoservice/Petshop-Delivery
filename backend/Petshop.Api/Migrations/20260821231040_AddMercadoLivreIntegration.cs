using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Petshop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMercadoLivreIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessTokenEncrypted",
                table: "MarketplaceIntegrations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenEncrypted",
                table: "MarketplaceIntegrations",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiresAtUtc",
                table: "MarketplaceIntegrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarketplaceOAuthStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceOAuthStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceOAuthStates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOAuthStates_CompanyId",
                table: "MarketplaceOAuthStates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceOAuthStates_State",
                table: "MarketplaceOAuthStates",
                column: "State",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceOAuthStates");

            migrationBuilder.DropColumn(
                name: "AccessTokenEncrypted",
                table: "MarketplaceIntegrations");

            migrationBuilder.DropColumn(
                name: "RefreshTokenEncrypted",
                table: "MarketplaceIntegrations");

            migrationBuilder.DropColumn(
                name: "TokenExpiresAtUtc",
                table: "MarketplaceIntegrations");
        }
    }
}
