using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeltaApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class SolicitationStatusRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remapeia os status antigos para o novo vocabulário.
            migrationBuilder.Sql("UPDATE \"Solicitations\" SET \"Status\" = 'FilaDeEspera' WHERE \"Status\" = 'Aberta';");
            migrationBuilder.Sql("UPDATE \"Solicitations\" SET \"Status\" = 'EmAtendimento' WHERE \"Status\" = 'EmAndamento';");
            migrationBuilder.Sql("UPDATE \"Solicitations\" SET \"Status\" = 'Finalizado' WHERE \"Status\" = 'Resolvida';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
