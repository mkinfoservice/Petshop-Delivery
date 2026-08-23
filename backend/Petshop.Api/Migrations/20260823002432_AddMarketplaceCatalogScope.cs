using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Petshop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceCatalogScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogSyncMode",
                table: "MarketplaceIntegrations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MarketplaceCategorySyncs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketplaceIntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceCategorySyncs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceCategorySyncs_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketplaceCategorySyncs_MarketplaceIntegrations_Marketplac~",
                        column: x => x.MarketplaceIntegrationId,
                        principalTable: "MarketplaceIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceProductMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketplaceIntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalItemId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LastSyncedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceProductMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceProductMappings_MarketplaceIntegrations_Marketpl~",
                        column: x => x.MarketplaceIntegrationId,
                        principalTable: "MarketplaceIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketplaceProductMappings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketplaceProductSelections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketplaceIntegrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceProductSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketplaceProductSelections_MarketplaceIntegrations_Market~",
                        column: x => x.MarketplaceIntegrationId,
                        principalTable: "MarketplaceIntegrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketplaceProductSelections_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceCategorySyncs_CategoryId",
                table: "MarketplaceCategorySyncs",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceCategorySyncs_MarketplaceIntegrationId_CategoryId",
                table: "MarketplaceCategorySyncs",
                columns: new[] { "MarketplaceIntegrationId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceProductMappings_MarketplaceIntegrationId_Product~",
                table: "MarketplaceProductMappings",
                columns: new[] { "MarketplaceIntegrationId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceProductMappings_ProductId",
                table: "MarketplaceProductMappings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceProductSelections_MarketplaceIntegrationId_Produ~",
                table: "MarketplaceProductSelections",
                columns: new[] { "MarketplaceIntegrationId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceProductSelections_ProductId",
                table: "MarketplaceProductSelections",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceCategorySyncs");

            migrationBuilder.DropTable(
                name: "MarketplaceProductMappings");

            migrationBuilder.DropTable(
                name: "MarketplaceProductSelections");

            migrationBuilder.DropColumn(
                name: "CatalogSyncMode",
                table: "MarketplaceIntegrations");
        }
    }
}
