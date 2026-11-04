using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HubPedidos.Infrastructure.Data;

public class HubPedidosDbContextFactory
    : IDesignTimeDbContextFactory<HubPedidosDbContext>
{
    public HubPedidosDbContext CreateDbContext(string[] args)
    {
        var caminhoConfiguracao = Path.Combine(
             Directory.GetCurrentDirectory(),
             "appsettings.json");

        using var documento = JsonDocument.Parse(
            File.ReadAllText(caminhoConfiguracao));

        var connectionString = documento.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("HubPedidos")
            .GetString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A conexão HubPedidos não foi encontrada.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<HubPedidosDbContext>();

        optionsBuilder.UseSqlServer(connectionString);

        return new HubPedidosDbContext(optionsBuilder.Options);
    }
}
