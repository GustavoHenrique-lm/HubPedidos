using System.Collections.Concurrent;
using System.Threading.Channels;
using HubPedidos.Application.Processing;
using Microsoft.Extensions.Options;

namespace HubPedidos.Infrastructure.Queue;

public sealed class PedidoQueue
{
    private readonly Channel<ProcessarPedidoJob> _channel;

    private readonly ConcurrentDictionary<Guid, byte>
        _pedidosRegistrados = new();

    private long _totalDescartados;

    public PedidoQueue(IOptions<PedidoQueueOptions> options)
    {
        var settings = options.Value;

        if (settings.Capacity <= 0)
        {
            throw new ArgumentException(
                "A capacidade da fila deve ser maior que zero.");
        }

        var channelOptions = new BoundedChannelOptions(
            settings.Capacity)
        {
            FullMode = settings.FullMode,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        _channel = Channel.CreateBounded<ProcessarPedidoJob>(
            channelOptions,
            job =>
            {
                // Remove o identificador do pedido descartado.
                _pedidosRegistrados.TryRemove(
                    job.PedidoId,
                    out _);

                Interlocked.Increment(
                    ref _totalDescartados);
            });
    }

    public ChannelReader<ProcessarPedidoJob> Reader =>
        _channel.Reader;

    public long TotalDescartados =>
        Interlocked.Read(ref _totalDescartados);

    public async ValueTask WriteAsync(
        ProcessarPedidoJob job,
        CancellationToken cancellationToken)
    {
        if (!_pedidosRegistrados.TryAdd(job.PedidoId, 0))
        {
            throw new InvalidOperationException(
                $"O pedido {job.PedidoId} já foi enviado à fila.");
        }

        try
        {
            await _channel.Writer.WriteAsync(
                job,
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _pedidosRegistrados.TryRemove(
                job.PedidoId,
                out _);

            throw;
        }
    }

    public bool TryWrite(ProcessarPedidoJob job)
    {
        if (!_pedidosRegistrados.TryAdd(job.PedidoId, 0))
        {
            return false;
        }

        if (_channel.Writer.TryWrite(job))
        {
            return true;
        }

        _pedidosRegistrados.TryRemove(
            job.PedidoId,
            out _);

        return false;
    }

    public void Complete()
    {
        _channel.Writer.TryComplete();
    }

    public bool PedidoJaRegistrado(Guid pedidoId)
    {
        return _pedidosRegistrados.ContainsKey(pedidoId);
    }
}
