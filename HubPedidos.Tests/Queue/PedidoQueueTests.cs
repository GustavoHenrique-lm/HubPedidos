using System.Threading.Channels;
using HubPedidos.Application.Processing;
using HubPedidos.Infrastructure.Queue;
using Microsoft.Extensions.Options;

namespace HubPedidos.Tests.Queue;

public class PedidoQueueTests
{
    [Fact]
    public void Fila_DeveRespeitarCapacidadeConfigurada()
    {
        var queue = CriarFila(capacity: 2);

        var job1 = CriarJob();
        var job2 = CriarJob();
        var job3 = CriarJob();

        Assert.True(queue.TryWrite(job1));
        Assert.True(queue.TryWrite(job2));
        Assert.False(queue.TryWrite(job3));

        Assert.Equal(2, queue.Reader.Count);
    }

    [Fact]
    public async Task Fila_EmModoWait_DeveAguardarEspaco()
    {
        var queue = CriarFila(capacity: 1);

        var job1 = CriarJob();
        var job2 = CriarJob();

        await queue.WriteAsync(
            job1,
            CancellationToken.None);

        var escrita = queue.WriteAsync(
            job2,
            CancellationToken.None).AsTask();

        Assert.False(escrita.IsCompleted);

        var removido = await queue.Reader.ReadAsync();

        Assert.Equal(job1.PedidoId, removido.PedidoId);

        await escrita.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(1, queue.Reader.Count);
    }

    [Fact]
    public async Task Fila_NaoDeveAceitarPedidoDuplicado()
    {
        var queue = CriarFila(capacity: 10);
        var job = CriarJob();

        await queue.WriteAsync(
            job,
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await queue.WriteAsync(
                job,
                CancellationToken.None).ConfigureAwait(false));

        Assert.Equal(1, queue.Reader.Count);
    }

    [Fact]
    public async Task Fila_DeveCancelarEscritaQuandoTokenForCancelado()
    {
        var queue = CriarFila(capacity: 1);

        var job1 = CriarJob();
        var job2 = CriarJob();

        await queue.WriteAsync(
            job1,
            CancellationToken.None);

        using var cts = new CancellationTokenSource();

        var escrita = queue.WriteAsync(
            job2,
            cts.Token).AsTask();

        Assert.False(escrita.IsCompleted);

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await escrita.ConfigureAwait(false));

        Assert.Equal(1, queue.Reader.Count);

        var removido = await queue.Reader.ReadAsync();

        Assert.Equal(job1.PedidoId, removido.PedidoId);

        await queue.WriteAsync(
            job2,
            CancellationToken.None);

        Assert.Equal(1, queue.Reader.Count);
    }

    [Fact]
    public void Fila_DropWrite_DeveDescartarNovoPedido()
    {
        var queue = CriarFila(
            capacity: 1,
            fullMode: BoundedChannelFullMode.DropWrite);

        var job1 = CriarJob();
        var job2 = CriarJob();

        Assert.True(queue.TryWrite(job1));
        Assert.True(queue.TryWrite(job2));

        Assert.Equal(1, queue.TotalDescartados);
        Assert.Equal(1, queue.Reader.Count);

        // O pedido descartado pode ser enviado novamente
        // depois que houver espaço.
        var removido = queue.Reader.TryRead(out var recebido);

        Assert.True(removido);
        Assert.Equal(job1.PedidoId, recebido!.PedidoId);

        Assert.True(queue.TryWrite(job2));
        Assert.Equal(1, queue.Reader.Count);
    }

    [Fact]
    public void Fila_DropOldest_DeveDescartarPedidoMaisAntigo()
    {
        var queue = CriarFila(
            capacity: 1,
            fullMode: BoundedChannelFullMode.DropOldest);

        var job1 = CriarJob();
        var job2 = CriarJob();

        Assert.True(queue.TryWrite(job1));
        Assert.True(queue.TryWrite(job2));

        Assert.Equal(1, queue.TotalDescartados);
        Assert.Equal(1, queue.Reader.Count);

        var removido = queue.Reader.TryRead(out var recebido);

        Assert.True(removido);
        Assert.Equal(job2.PedidoId, recebido!.PedidoId);

        // O identificador do pedido descartado foi liberado.
        Assert.True(queue.TryWrite(job1));
    }

    private static PedidoQueue CriarFila(
        int capacity,
        BoundedChannelFullMode fullMode =
            BoundedChannelFullMode.Wait)
    {
        var options = Options.Create(
            new PedidoQueueOptions
            {
                Capacity = capacity,
                Consumers = 1,
                DatabaseConcurrencyLimit = 1,
                FullMode = fullMode
            });

        return new PedidoQueue(options);
    }

    private static ProcessarPedidoJob CriarJob()
    {
        return new ProcessarPedidoJob(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
    }
}
