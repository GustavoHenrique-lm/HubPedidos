
using HubPedidos.Infrastructure.Queue;

namespace HubPedidos.Tests.Queue;

public class PedidoConcurrencyLimiterTests
{
    [Fact]
    public async Task Limiter_NaoDeveUltrapassarLimiteConfigurado()
    {
        var options = new PedidoQueueOptions
        {
            DatabaseConcurrencyLimit = 2
        };

        var limiter = new PedidoConcurrencyLimiter(options);

        var tarefas = Enumerable.Range(0, 10)
            .Select(async _ =>
            {
                await limiter.AguardarPermissaoAsync(
                    CancellationToken.None).ConfigureAwait(false);

                try
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
                finally
                {
                    limiter.Liberar();
                }
            });

        await Task.WhenAll(tarefas);

        var metricas = limiter.ObterMetricas();

        var json = System.Text.Json.JsonSerializer
            .Serialize(metricas);

        using var documento =
            System.Text.Json.JsonDocument.Parse(json);

        var maximo = documento.RootElement
            .GetProperty("MaximoObservado")
            .GetInt32();

        var ativas = documento.RootElement
            .GetProperty("OperacoesAtivas")
            .GetInt32();

        Assert.True(maximo <= 2);
        Assert.Equal(2, maximo);
        Assert.Equal(0, ativas);
    }
}
