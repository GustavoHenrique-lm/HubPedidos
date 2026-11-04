
namespace HubPedidos.Application.Processing;

public sealed record ProcessarPedidoJob(
    Guid PedidoId,
    DateTimeOffset DataSolicitacao
);
