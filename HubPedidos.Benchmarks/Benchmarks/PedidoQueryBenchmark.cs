
using BenchmarkDotNet.Attributes;
using HubPedidos.Application.Contracts.Consultas;
using HubPedidos.Infrastructure.Data;
using HubPedidos.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class PedidoQueryBenchmark
{
    private DbContextOptions<HubPedidosDbContext> _options = null!;

    private readonly PedidoConsultaRequest _request =
        new(null, null, 1, 5);

    [GlobalSetup]
    public async Task Setup()
    {
        var connectionString =
            "Server=.\\SQLEXPRESS;Database=HubPedidosDb;Trusted_Connection=True;TrustServerCertificate=True";

        _options = new DbContextOptionsBuilder<HubPedidosDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var context =
            new HubPedidosDbContext(_options);

        if (!await context.Database.CanConnectAsync().ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "Não foi possível conectar ao SQL Server.");
        }

        var service = new PedidoQueryService(context);

        var convencional =
            await service.ConsultarAsync(_request).ConfigureAwait(false);

        var compilada =
            await service.ConsultarCompiladoAsync(_request).ConfigureAwait(false);

        if (convencional.Total != compilada.Total ||
            !convencional.Pedidos.SequenceEqual(compilada.Pedidos))
        {
            throw new InvalidOperationException(
                "As consultas retornaram resultados diferentes.");
        }

        Console.WriteLine(
            $"Banco conectado. Total de pedidos: {convencional.Total}");
    }

    [Benchmark(Baseline = true)]
    public async Task<int> ConsultaConvencional()
    {
        using var context =
          new HubPedidosDbContext(_options);

        var service = new PedidoQueryService(context);

        var resultado =
            await service.ConsultarAsync(_request).ConfigureAwait(false);

        return resultado.Pedidos.Count;
    }

    [Benchmark]
    public async Task<int> ConsultaCompilada()
    {
        using var context =
             new HubPedidosDbContext(_options);

        var service = new PedidoQueryService(context);

        var resultado =
            await service.ConsultarCompiladoAsync(_request).ConfigureAwait(false);

        return resultado.Pedidos.Count;
    }
}
