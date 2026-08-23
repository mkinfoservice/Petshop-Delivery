using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Petshop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductDescriptionGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DescriptionProcessed",
                table: "ProductEnrichmentResults",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableDescriptionGeneration",
                table: "EnrichmentConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DescriptionsGenerated",
                table: "EnrichmentBatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProductDescriptionSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalDescription = table.Column<string>(type: "text", nullable: true),
                    SuggestedDescription = table.Column<string>(type: "text", nullable: false),
                    ModelUsed = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDescriptionSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductDescriptionSuggestions_EnrichmentBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "EnrichmentBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductDescriptionSuggestions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductDescriptionSuggestions_BatchId",
                table: "ProductDescriptionSuggestions",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductDescriptionSuggestions_CompanyId_Status",
                table: "ProductDescriptionSuggestions",
                columns: new[] { "CompanyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductDescriptionSuggestions_ProductId",
                table: "ProductDescriptionSuggestions",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductDescriptionSuggestions");

            migrationBuilder.DropColumn(
                name: "DescriptionProcessed",
                table: "ProductEnrichmentResults");

            migrationBuilder.DropColumn(
                name: "EnableDescriptionGeneration",
                table: "EnrichmentConfigs");

            migrationBuilder.DropColumn(
                name: "DescriptionsGenerated",
                table: "EnrichmentBatches");
        }
    }
}
