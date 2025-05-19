using Microsoft.EntityFrameworkCore.Migrations;

namespace secNET.Migrations
{
    public partial class AddChecklistViolationsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChecklistViolations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CCTVLogId = table.Column<int>(nullable: false),
                    Question = table.Column<string>(nullable: true),
                    Section = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistViolations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistViolations_CCTVLogId_Question",
                table: "ChecklistViolations",
                columns: new[] { "CCTVLogId", "Question" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistViolations");
        }
    }
}
