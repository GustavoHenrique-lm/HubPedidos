
using HubPedidos.Domain.Enums;

namespace HubPedidos.Application.Contracts.V2;

public record PedidoV2Response(
    Guid PedidoId,
    CategoriaPedido Classificacao,
    decimal ValorTotal,
    string Mensagem
);
