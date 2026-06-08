using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagerWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEventsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserSetores",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "SetorId",
                table: "UserSetores",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AssignedAt",
                table: "UserSetores",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "UserSetores",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "TIMESTAMP(7)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "NVARCHAR2(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "Users",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Users",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Timesheets",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Timesheets",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Timesheets",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Timesheets",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Timesheets",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Timesheets",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "ApprovedById",
                table: "Timesheets",
                type: "NUMBER(10)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "Timesheets",
                type: "TIMESTAMP(7)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Timesheets",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<decimal>(
                name: "WorkHours",
                table: "TimesheetEntries",
                type: "DECIMAL(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "TimesheetId",
                table: "TimesheetEntries",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "TimesheetEntries",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "TimesheetEntries",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "TimesheetEntries",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Setores",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Setores",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Setores",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Setores",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Setores",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ProjectUserCosts",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProjectUserCosts",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectUserCosts",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ProjectUserCosts",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "CostPerHour",
                table: "ProjectUserCosts",
                type: "DECIMAL(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectUserCosts",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProjectTasks",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "ProjectTasks",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ProjectTasks",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectTasks",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "Progress",
                table: "ProjectTasks",
                type: "DECIMAL(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "ProjectTasks",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<int>(
                name: "EstimatedHours",
                table: "ProjectTasks",
                type: "NUMBER(10)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "ProjectTasks",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ProjectTasks",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ProjectTasks",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedTo",
                table: "ProjectTasks",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActualHours",
                table: "ProjectTasks",
                type: "NUMBER(10)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectTasks",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<string>(
                name: "ToStatus",
                table: "ProjectStatusHistories",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "ProjectStatusHistories",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectStatusHistories",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "FromStatus",
                table: "ProjectStatusHistories",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "ProjectStatusHistories",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "ProjectStatusHistories",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectStatusHistories",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Projects",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Projects",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Projects",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "SetorId",
                table: "Projects",
                type: "NUMBER(10)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "Projects",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "Projects",
                type: "NUMBER(10)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Manager",
                table: "Projects",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FreshDeskId",
                table: "Projects",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Projects",
                type: "TIMESTAMP(7)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Projects",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Projects",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "Projects",
                type: "TIMESTAMP(7)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Projects",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<string>(
                name: "ToOwner",
                table: "ProjectOwnerHistories",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectOwnerHistories",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "FromOwner",
                table: "ProjectOwnerHistories",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "ProjectOwnerHistories",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "ProjectOwnerHistories",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectOwnerHistories",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ProjectMembers",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "ProjectMembers",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectMembers",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "ProjectMembers",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectMembers",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<string>(
                name: "ToManager",
                table: "ProjectManagerHistories",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectManagerHistories",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "FromManager",
                table: "ProjectManagerHistories",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "ProjectManagerHistories",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "ProjectManagerHistories",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectManagerHistories",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Comments",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "TIMESTAMP(7)",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Comments",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "Comments",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Comments",
                type: "NUMBER(10)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    UserId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Title = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Description = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Date = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    StartTime = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    EndTime = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ProjectId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    IsApplicableToProject = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Events_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_ProjectId",
                table: "Events",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_UserId",
                table: "Events",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "UserSetores",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<int>(
                name: "SetorId",
                table: "UserSetores",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AssignedAt",
                table: "UserSetores",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "UserSetores",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Users",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Users",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "TEXT(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "Users",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Timesheets",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Timesheets",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Timesheets",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "RejectionReason",
                table: "Timesheets",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "Timesheets",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Timesheets",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<int>(
                name: "ApprovedById",
                table: "Timesheets",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ApprovedAt",
                table: "Timesheets",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Timesheets",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<decimal>(
                name: "WorkHours",
                table: "TimesheetEntries",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "TimesheetId",
                table: "TimesheetEntries",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "TimesheetEntries",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "TimesheetEntries",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "TimesheetEntries",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Setores",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Setores",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Setores",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Setores",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Setores",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ProjectUserCosts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProjectUserCosts",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectUserCosts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ProjectUserCosts",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CostPerHour",
                table: "ProjectUserCosts",
                type: "TEXT",
                precision: 10,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(10,2)",
                oldPrecision: 10,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectUserCosts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProjectTasks",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "ProjectTasks",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ProjectTasks",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectTasks",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Progress",
                table: "ProjectTasks",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "ProjectTasks",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<int>(
                name: "EstimatedHours",
                table: "ProjectTasks",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "ProjectTasks",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ProjectTasks",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ProjectTasks",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedTo",
                table: "ProjectTasks",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActualHours",
                table: "ProjectTasks",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectTasks",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<string>(
                name: "ToStatus",
                table: "ProjectStatusHistories",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "ProjectStatusHistories",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectStatusHistories",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<string>(
                name: "FromStatus",
                table: "ProjectStatusHistories",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "ProjectStatusHistories",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "ProjectStatusHistories",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectStatusHistories",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Projects",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<int>(
                name: "SetorId",
                table: "Projects",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Priority",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "Projects",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Manager",
                table: "Projects",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FreshDeskId",
                table: "Projects",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "Projects",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Projects",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "Projects",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<string>(
                name: "ToOwner",
                table: "ProjectOwnerHistories",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectOwnerHistories",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<string>(
                name: "FromOwner",
                table: "ProjectOwnerHistories",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "ProjectOwnerHistories",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "ProjectOwnerHistories",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectOwnerHistories",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "ProjectMembers",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "ProjectMembers",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectMembers",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "JoinedAt",
                table: "ProjectMembers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectMembers",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<string>(
                name: "ToManager",
                table: "ProjectManagerHistories",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "ProjectManagerHistories",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<string>(
                name: "FromManager",
                table: "ProjectManagerHistories",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "ChangedBy",
                table: "ProjectManagerHistories",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "ProjectManagerHistories",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ProjectManagerHistories",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");

            migrationBuilder.AlterColumn<int>(
                name: "ProjectId",
                table: "Comments",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Comments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "TIMESTAMP(7)");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "Comments",
                type: "TEXT(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "Author",
                table: "Comments",
                type: "TEXT(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Comments",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "NUMBER(10)")
                .OldAnnotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1");
        }
    }
}
