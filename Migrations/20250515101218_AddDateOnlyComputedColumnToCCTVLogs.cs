using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace secNET.Migrations
{
    public partial class AddDateOnlyComputedColumnToCCTVLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOnly",
                table: "CCTVLogs",
                nullable: false,
                computedColumnSql: "CAST([Date] AS DATE)");

            migrationBuilder.CreateIndex(
                name: "IX_CCTVLogs_DateOnly",
                table: "CCTVLogs",
                column: "DateOnly");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CCTVLogs_DateOnly",
                table: "CCTVLogs");

            migrationBuilder.DropColumn(
                name: "DateOnly",
                table: "CCTVLogs");
        }
    }
}
