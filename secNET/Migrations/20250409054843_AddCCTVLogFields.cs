using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace secNET.Migrations
{
    public partial class AddCCTVLogFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BranchManager",
                table: "CCTVLogs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchManagerNotes",
                table: "CCTVLogs",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfReportSeen",
                table: "CCTVLogs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatorName",
                table: "CCTVLogs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportSeenBy",
                table: "CCTVLogs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecuritySupervisor",
                table: "CCTVLogs",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecuritySupervisorNotes",
                table: "CCTVLogs",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchManager",
                table: "CCTVLogs");

            migrationBuilder.DropColumn(
                name: "BranchManagerNotes",
                table: "CCTVLogs");

            migrationBuilder.DropColumn(
                name: "DateOfReportSeen",
                table: "CCTVLogs");

            migrationBuilder.DropColumn(
                name: "OperatorName",
                table: "CCTVLogs");

            migrationBuilder.DropColumn(
                name: "ReportSeenBy",
                table: "CCTVLogs");

            migrationBuilder.DropColumn(
                name: "SecuritySupervisor",
                table: "CCTVLogs");

            migrationBuilder.DropColumn(
                name: "SecuritySupervisorNotes",
                table: "CCTVLogs");
        }
    }
}
