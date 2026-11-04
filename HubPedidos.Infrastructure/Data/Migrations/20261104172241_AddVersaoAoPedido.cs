using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubPedidos.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVersaoAoPedido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Versao",
                table: "Pedidos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Versao",
                table: "Pedidos");
        }
    }
}
