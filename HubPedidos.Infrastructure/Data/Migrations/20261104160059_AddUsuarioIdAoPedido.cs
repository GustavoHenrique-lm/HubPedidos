using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubPedidos.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsuarioIdAoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioId",
                table: "Pedidos",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Pedidos");
        }
    }
}
