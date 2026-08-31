using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagerWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddKubernetesNotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ValorAnterior",
                table: "KubernetesAuditLogs",
                type: "CLOB",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValorNovo",
                table: "KubernetesAuditLogs",
                type: "CLOB",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KubernetesDeploymentNotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    Namespace = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Deployment = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Titulo = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    Memo = table.Column<string>(type: "CLOB", nullable: true),
                    AtualizadoPor = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    AtualizadoPorNome = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    AtualizadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KubernetesDeploymentNotas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KubernetesDeploymentNotas_Namespace_Deployment",
                table: "KubernetesDeploymentNotas",
                columns: new[] { "Namespace", "Deployment" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KubernetesDeploymentNotas");

            migrationBuilder.DropColumn(
                name: "ValorAnterior",
                table: "KubernetesAuditLogs");

            migrationBuilder.DropColumn(
                name: "ValorNovo",
                table: "KubernetesAuditLogs");
        }
    }
}
