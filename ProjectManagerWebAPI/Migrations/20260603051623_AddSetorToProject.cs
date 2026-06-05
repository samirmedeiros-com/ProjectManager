using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagerWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSetorToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SetorId",
                table: "Projects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SetorId",
                table: "Projects",
                column: "SetorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Setores_SetorId",
                table: "Projects",
                column: "SetorId",
                principalTable: "Setores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Setores_SetorId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_SetorId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SetorId",
                table: "Projects");
        }
    }
}
