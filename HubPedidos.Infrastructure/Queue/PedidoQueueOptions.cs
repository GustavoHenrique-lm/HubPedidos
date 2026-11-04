using System.Threading.Channels;

namespace HubPedidos.Infrastructure.Queue;

public sealed class PedidoQueueOptions
{
    public int Capacity { get; set; } = 100;

    public int Consumers { get; set; } = 4;

    public int DatabaseConcurrencyLimit { get; set; } = 2;

    public BoundedChannelFullMode FullMode { get; set; } =
        BoundedChannelFullMode.Wait;
}
