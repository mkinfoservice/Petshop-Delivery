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
            // Garante que a tabela existe antes do DELETE — em bancos novos (Aiven/CI)
            // ela pode não ter sido criada ainda via Program.cs startup SQL.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "CompanyFeatureOverrides" (
                    "Id"           uuid NOT NULL,
                    "CompanyId"    uuid NOT NULL,
                    "FeatureKey"   character varying(80) NOT NULL,
                    "IsEnabled"    boolean NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone NOT NULL,
                    CONSTRAINT "PK_CompanyFeatureOverrides" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_CompanyFeatureOverrides_Companies_CompanyId"
                        FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_CompanyFeatureOverrides_CompanyId_FeatureKey"
                    ON "CompanyFeatureOverrides" ("CompanyId", "FeatureKey");
                """);

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
