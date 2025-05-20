using Microsoft.EntityFrameworkCore.Migrations;

namespace secNET.Migrations
{
    public partial class AddIndexesToCCTVLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CCTVLogs_Date",
                table: "CCTVLogs",
                column: "Date");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CCTVLogs_Date",
                table: "CCTVLogs");
        }
    }
}
