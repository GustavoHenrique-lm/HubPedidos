
namespace HubPedidos.Api.Hubs;

public sealed record PedidoAtualizadoEvent(
    Guid PedidoId,
    long Versao,
    DateTimeOffset AtualizadoEmUtc,
    bool Prioritario
);

public interface IPedidosClient
{
    public Task PedidoAtualizado(PedidoAtualizadoEvent evento);
}

public static class PedidoGrupo
{
    public static string Nome(Guid pedidoId, Guid usuarioId)
    {
        return $"pedido:{pedidoId}:usuario:{usuarioId}";
    }
}
