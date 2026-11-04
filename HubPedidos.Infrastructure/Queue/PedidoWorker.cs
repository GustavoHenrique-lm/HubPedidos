using HubPedidos.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HubPedidos.Infrastructure.Queue;

public sealed class PedidoWorker : BackgroundService
{
    private readonly PedidoQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PedidoConcurrencyLimiter _limiter;
    private readonly PedidoQueueMetrics _metrics;
    private readonly ILogger<PedidoWorker> _logger;
    private readonly int _quantidadeConsumidores;

    public PedidoWorker(
        PedidoQueue queue,
        IServiceScopeFactory scopeFactory,
        PedidoConcurrencyLimiter limiter,
        PedidoQueueMetrics metrics,
        ILogger<PedidoWorker> logger,
        PedidoQueueOptions options)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _limiter = limiter;
        _metrics = metrics;
        _logger = logger;
        _quantidadeConsumidores = options.Consumers;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Worker de pedidos iniciado com {Quantidade} consumidores.",
            _quantidadeConsumidores);

        var consumidores = Enumerable
            .Range(1, _quantidadeConsumidores)
            .Select(id => ConsumirAsync(id));

        await Task.WhenAll(consumidores).ConfigureAwait(false);

        _logger.LogInformation(
            "Todos os consumidores foram encerrados.");
    }

    private async Task ConsumirAsync(int consumidorId)
    {
        try
        {
            await foreach (
                var job in _queue.Reader.ReadAllAsync(
                    CancellationToken.None).ConfigureAwait(false))
            {
                var inicio = DateTimeOffset.UtcNow;
                var tempoEspera = inicio - job.DataSolicitacao;

                _metrics.RegistrarInicio(job.PedidoId);

                try
                {
                    await _limiter.AguardarPermissaoAsync(
                        CancellationToken.None).ConfigureAwait(false);

                    try
                    {
                        using var scope =
                            _scopeFactory.CreateScope();

                        var service = scope.ServiceProvider
                            .GetRequiredService<IProcessarPedidoService>();

                        await service.ProcessarAsync(
                            job,
                            CancellationToken.None).ConfigureAwait(false);
                    }
                    finally
                    {
                        _limiter.Liberar();
                    }

                    var duracao =
                        DateTimeOffset.UtcNow - inicio;

                    _metrics.RegistrarProcessado(
                        job.PedidoId,
                        tempoEspera);

                    _logger.LogInformation(
                        "Consumidor {Consumidor}: pedido {PedidoId} processado em {Duracao} ms.",
                        consumidorId,
                        job.PedidoId,
                        duracao.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    _metrics.RegistrarFalha(job.PedidoId);

                    _logger.LogError(
                        ex,
                        "Consumidor {Consumidor}: falha ao processar pedido {PedidoId}.",
                        consumidorId,
                        job.PedidoId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro inesperado no consumidor {Consumidor}.",
                consumidorId);
        }

        _logger.LogInformation(
            "Consumidor {Consumidor} encerrado.",
            consumidorId);
    }

    public override async Task StopAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Encerrando worker. Concluindo a fila para drenar os pedidos pendentes.");

        _queue.Complete();

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
