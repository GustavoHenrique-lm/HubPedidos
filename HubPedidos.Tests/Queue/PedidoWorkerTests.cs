using HubPedidos.Application.Contracts;
using HubPedidos.Application.Processing;
using HubPedidos.Infrastructure.Queue;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HubPedidos.Tests.Queue;

public class PedidoWorkerTests
{
    [Fact]
    public async Task Worker_DeveContinuarAposFalhaDeUmPedido()
    {
        // Arrange
        var options = new PedidoQueueOptions
        {
            Capacity = 10,
            Consumers = 1,
            DatabaseConcurrencyLimit = 1
        };

        var queue = new PedidoQueue(Options.Create(options));
        var metrics = new PedidoQueueMetrics();
        var limiter = new PedidoConcurrencyLimiter(options);

        var primeiroId = Guid.NewGuid();
        var segundoId = Guid.NewGuid();

        var processador = new ProcessadorSimulado(
            falharPrimeiraChamada: true);

        var services = new ServiceCollection();

        services.AddSingleton<IProcessarPedidoService>(
            processador);

        using var provider = services.BuildServiceProvider();

        using var loggerFactory = LoggerFactory.Create(
            builder => { });

        var logger = loggerFactory.CreateLogger<PedidoWorker>();

        var worker = new PedidoWorker(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            limiter,
            metrics,
            logger,
            options);

        await queue.WriteAsync(
            new ProcessarPedidoJob(
                primeiroId,
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        await queue.WriteAsync(
            new ProcessarPedidoJob(
                segundoId,
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        metrics.RegistrarRecebido();
        metrics.RegistrarAceito(primeiroId);

        metrics.RegistrarRecebido();
        metrics.RegistrarAceito(segundoId);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Aguarda a confirmação de que o primeiro pedido começou.
        await processador.PrimeiraChamada.Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        // Encerra o worker e permite a drenagem da fila.
        await worker.StopAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, processador.QuantidadeChamadas);
        Assert.Equal(1, processador.QuantidadeFalhas);
        Assert.Equal(1, processador.QuantidadeSucessos);

        var resumo = System.Text.Json.JsonSerializer
            .Serialize(metrics.ObterResumo());

        using var documento =
            System.Text.Json.JsonDocument.Parse(resumo);

        Assert.Equal(
            1,
            documento.RootElement
                .GetProperty("Falhas")
                .GetInt32());

        Assert.Equal(
            1,
            documento.RootElement
                .GetProperty("Processados")
                .GetInt32());
    }

    [Fact]
    public async Task Worker_DeveProcessarPedidosPendentesAoEncerrar()
    {
        // Arrange
        var options = new PedidoQueueOptions
        {
            Capacity = 10,
            Consumers = 1,
            DatabaseConcurrencyLimit = 1
        };

        var queue = new PedidoQueue(Options.Create(options));
        var metrics = new PedidoQueueMetrics();
        var limiter = new PedidoConcurrencyLimiter(options);

        var processador = new ProcessadorSimulado(
            falharPrimeiraChamada: false);

        var services = new ServiceCollection();

        services.AddSingleton<IProcessarPedidoService>(
            processador);

        using var provider = services.BuildServiceProvider();

        using var loggerFactory = LoggerFactory.Create(
            builder => { });

        var logger = loggerFactory.CreateLogger<PedidoWorker>();

        var worker = new PedidoWorker(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            limiter,
            metrics,
            logger,
            options);

        for (int i = 0; i < 5; i++)
        {
            var id = Guid.NewGuid();

            await queue.WriteAsync(
                new ProcessarPedidoJob(
                    id,
                    DateTimeOffset.UtcNow),
                CancellationToken.None);

            metrics.RegistrarRecebido();
            metrics.RegistrarAceito(id);
        }

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Garante que o consumidor começou antes do encerramento.
        await processador.PrimeiraChamada.Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        await worker.StopAsync(CancellationToken.None);

        // Assert
        Assert.Equal(5, processador.QuantidadeChamadas);
        Assert.Equal(0, processador.QuantidadeFalhas);
        Assert.Equal(5, processador.QuantidadeSucessos);
    }

    private sealed class ProcessadorSimulado
        : IProcessarPedidoService
    {
        private readonly bool _falharPrimeiraChamada;

        public int QuantidadeChamadas { get; private set; }

        public int QuantidadeFalhas { get; private set; }

        public int QuantidadeSucessos { get; private set; }

        public TaskCompletionSource<bool> PrimeiraChamada { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ProcessadorSimulado(
            bool falharPrimeiraChamada = true)
        {
            _falharPrimeiraChamada = falharPrimeiraChamada;
        }

        public Task ProcessarAsync(
            ProcessarPedidoJob job,
            CancellationToken cancellationToken)
        {
            QuantidadeChamadas++;

            // Sinaliza que o consumidor começou a processar.
            PrimeiraChamada.TrySetResult(true);

            if (_falharPrimeiraChamada &&
                QuantidadeChamadas == 1)
            {
                QuantidadeFalhas++;

                throw new InvalidOperationException(
                    "Falha simulada no processamento.");
            }

            QuantidadeSucessos++;

            return Task.CompletedTask;
        }
    }
}
