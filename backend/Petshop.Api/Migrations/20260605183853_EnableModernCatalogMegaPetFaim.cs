using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Petshop.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnableModernCatalogMegaPetFaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove qualquer override legacy_catalog_experience=true para a megapetfaim.
            // O catálogo moderno é o default do frontend (ausência do flag → moderno).
            // Se o override existia em produção, este DELETE garante que a loja use o ModernPublicCatalog.
            migrationBuilder.Sql("""
                DELETE FROM "CompanyFeatureOverrides" cfo
                USING "Companies" c
                WHERE cfo."CompanyId" = c."Id"
                  AND c."Slug" = 'megapetfaim'
                  AND cfo."FeatureKey" = 'legacy_catalog_experience';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
