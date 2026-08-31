using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagerWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddKubernetesAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KubernetesAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    UserId = table.Column<int>(type: "NUMBER(10)", nullable: true),
                    UserEmail = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    UserName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Acao = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Namespace = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    Deployment = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    Sucesso = table.Column<int>(type: "NUMBER(1)", nullable: false),
                    Detalhe = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    IpOrigem = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KubernetesAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KubernetesAuditLogs_CriadoEm",
                table: "KubernetesAuditLogs",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_KubernetesAuditLogs_Namespace_Deployment_CriadoEm",
                table: "KubernetesAuditLogs",
                columns: new[] { "Namespace", "Deployment", "CriadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KubernetesAuditLogs");
        }
    }
}
