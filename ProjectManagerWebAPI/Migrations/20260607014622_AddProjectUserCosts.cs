using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagerWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectUserCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_Projects_ProjectId",
                table: "Timesheets");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_Users_CreatedById",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_CreatedById",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_ProjectId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "WeekEndDate",
                table: "Timesheets");

            migrationBuilder.RenameColumn(
                name: "WeekStartDate",
                table: "Timesheets",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "DayOfWeek",
                table: "TimesheetEntries",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetEntries_TimesheetId_DayOfWeek",
                table: "TimesheetEntries",
                newName: "IX_TimesheetEntries_TimesheetId_ProjectId");

            migrationBuilder.CreateTable(
                name: "ProjectUserCosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    ProjectId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    UserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    CostPerHour = table.Column<decimal>(type: "DECIMAL(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectUserCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectUserCosts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectUserCosts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_ProjectId",
                table: "TimesheetEntries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserCosts_ProjectId_UserId",
                table: "ProjectUserCosts",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectUserCosts_UserId",
                table: "ProjectUserCosts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimesheetEntries_Projects_ProjectId",
                table: "TimesheetEntries",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimesheetEntries_Projects_ProjectId",
                table: "TimesheetEntries");

            migrationBuilder.DropTable(
                name: "ProjectUserCosts");

            migrationBuilder.DropIndex(
                name: "IX_TimesheetEntries_ProjectId",
                table: "TimesheetEntries");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Timesheets",
                newName: "WeekStartDate");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "TimesheetEntries",
                newName: "DayOfWeek");

            migrationBuilder.RenameIndex(
                name: "IX_TimesheetEntries_TimesheetId_ProjectId",
                table: "TimesheetEntries",
                newName: "IX_TimesheetEntries_TimesheetId_DayOfWeek");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Timesheets",
                type: "NUMBER(10)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Timesheets",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "WeekEndDate",
                table: "Timesheets",
                type: "TIMESTAMP(7)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_CreatedById",
                table: "Timesheets",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_ProjectId",
                table: "Timesheets",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_Projects_ProjectId",
                table: "Timesheets",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_Users_CreatedById",
                table: "Timesheets",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
