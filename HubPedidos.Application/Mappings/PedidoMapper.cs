
using HubPedidos.Application.Contracts;
using HubPedidos.Domain.Entities;
using HubPedidos.Domain.ValueObjects;

namespace HubPedidos.Application.Mappings;

public static class PedidoMapper
{
    public static Pedido ParaDominio(CriarPedidoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Itens is null || request.Itens.Count == 0)
        {
            throw new ArgumentException("Informe pelo menos um item.");
        }

        var id = PedidoId.Criar(request.Id);

        var itens = request.Itens.Select(item =>
        {
            if (item is null)
            {
                throw new ArgumentException("Existe um item inválido.");
            }

            return new ItemPedido(
                item.Produto,
                item.Quantidade,
                item.PrecoUnitario
            );
        }).ToList();

        var metadados = new MetadadosPedido(
            string.IsNullOrWhiteSpace(request.Canal)
                ? "Sistema"
                : request.Canal,
            request.Observacao ?? string.Empty
        );

        return new Pedido(
            id,
            itens,
            request.Regiao,
            request.Prioritario,
            metadados
        );
    }

    public static PedidoResponse ParaResponse(
        Pedido pedido,
        HubPedidos.Domain.Enums.CategoriaPedido categoria)
    {
        return new PedidoResponse(
            pedido.Id.Value,
            categoria,
            pedido.ValorTotal,
            $"Pedido classificado como {categoria}."
        );
    }
}
