using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagerWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class ConvertUserSetorToManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Setores_SetorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SetorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SetorId",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "UserSetores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SetorId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSetores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSetores_Setores_SetorId",
                        column: x => x.SetorId,
                        principalTable: "Setores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSetores_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSetores_SetorId",
                table: "UserSetores",
                column: "SetorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSetores_UserId_SetorId",
                table: "UserSetores",
                columns: new[] { "UserId", "SetorId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSetores");

            migrationBuilder.AddColumn<int>(
                name: "SetorId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SetorId",
                table: "Users",
                column: "SetorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Setores_SetorId",
                table: "Users",
                column: "SetorId",
                principalTable: "Setores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
