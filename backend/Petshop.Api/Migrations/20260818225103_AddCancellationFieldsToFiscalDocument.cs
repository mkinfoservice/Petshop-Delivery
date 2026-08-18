using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Petshop.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationFieldsToFiscalDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelProtocol",
                table: "FiscalDocuments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                table: "FiscalDocuments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "FiscalDocuments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelProtocol",
                table: "FiscalDocuments");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                table: "FiscalDocuments");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "FiscalDocuments");
        }
    }
}
