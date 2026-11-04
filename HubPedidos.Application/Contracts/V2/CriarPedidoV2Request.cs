
namespace HubPedidos.Application.Contracts.V2;

public record CriarPedidoV2Request(
    Guid PedidoId,
    List<ItemPedidoV2Request> Itens,
    string Regiao,
    bool Prioritario,
    string? Canal = "Sistema",
    string? Observacao = ""
);

public record ItemPedidoV2Request(
    string Produto,
    int Quantidade,
    decimal PrecoUnitario
);
