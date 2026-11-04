
namespace HubPedidos.Application.Contracts;

public record CriarPedidoRequest(
    Guid Id,
    List<ItemPedidoRequest> Itens,
    string Regiao,
    bool Prioritario,
    string? Canal = "Sistema",
    string? Observacao = ""
);

public record ItemPedidoRequest(
    string Produto,
    int Quantidade,
    decimal PrecoUnitario
);
