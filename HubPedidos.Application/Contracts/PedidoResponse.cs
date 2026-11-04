using HubPedidos.Domain.Enums;

namespace HubPedidos.Application.Contracts;

public record PedidoResponse(
    Guid Id,
    CategoriaPedido Categoria,
    decimal ValorTotal,
    string Mensagem
);
