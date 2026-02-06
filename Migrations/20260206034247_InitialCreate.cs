using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Carpet_Work_Progress.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgressLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Barcode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProgressDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NormalPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    OtPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TotalPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ImagePath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressLogs_Barcode",
                table: "ProgressLogs",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressLogs_ProgressDate",
                table: "ProgressLogs",
                column: "ProgressDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgressLogs");
        }
    }
}
