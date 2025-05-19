using Microsoft.EntityFrameworkCore.Migrations;

namespace secNET.Migrations
{
    public partial class AddIndexesToIncidentReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_IncidentReports_IncidentDateTime",
                table: "IncidentReports",
                column: "IncidentDateTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IncidentReports_IncidentDateTime",
                table: "IncidentReports");
        }
    }
}
