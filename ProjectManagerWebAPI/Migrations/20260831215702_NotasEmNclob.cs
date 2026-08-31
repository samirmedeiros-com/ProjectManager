using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectManagerWebAPI.Migrations
{
    /// <summary>
    /// Passa as colunas de texto livre da Gestão Kubernetes de CLOB para NCLOB.
    ///
    /// O CLOB guarda no charset da base de dados, que aqui **não é Unicode**: um travessão "—"
    /// escrito no título chegava ao registo como "¿". O NCLOB usa o charset nacional
    /// (AL16UTF16), o mesmo dos NVARCHAR2 que o resto do esquema já usa.
    ///
    /// O Oracle **não deixa** converter CLOB em NCLOB com ALTER (ORA-22859), por isso as
    /// colunas são apagadas e recriadas. É seguro aqui porque foram criadas na migração
    /// imediatamente anterior e ainda não têm dados de utilização real.
    /// </summary>
    public partial class NotasEmNclob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Memo", table: "KubernetesDeploymentNotas");
            migrationBuilder.DropColumn(name: "ValorAnterior", table: "KubernetesAuditLogs");
            migrationBuilder.DropColumn(name: "ValorNovo", table: "KubernetesAuditLogs");

            migrationBuilder.AddColumn<string>(
                name: "Memo", table: "KubernetesDeploymentNotas", type: "NCLOB", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValorAnterior", table: "KubernetesAuditLogs", type: "NCLOB", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValorNovo", table: "KubernetesAuditLogs", type: "NCLOB", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Memo", table: "KubernetesDeploymentNotas");
            migrationBuilder.DropColumn(name: "ValorAnterior", table: "KubernetesAuditLogs");
            migrationBuilder.DropColumn(name: "ValorNovo", table: "KubernetesAuditLogs");

            migrationBuilder.AddColumn<string>(
                name: "Memo", table: "KubernetesDeploymentNotas", type: "CLOB", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValorAnterior", table: "KubernetesAuditLogs", type: "CLOB", nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValorNovo", table: "KubernetesAuditLogs", type: "CLOB", nullable: true);
        }
    }
}
