using HubPedidos.Domain.Entities;
using HubPedidos.Domain.Enums;

namespace HubPedidos.Domain.Services;

public static class ClassificadorPedido
{
    public static CategoriaPedido Classificar(Pedido? pedido)
    {
        return pedido switch
        {
            null => CategoriaPedido.Invalido,

            { Itens.Count: 0 } => CategoriaPedido.Invalido,

            { Prioritario: true } => CategoriaPedido.Prioritario,

            { Regiao: "SP", ValorTotal: >= 1000m }
                => CategoriaPedido.Prioritario,

            { ValorTotal: <= 0 } => CategoriaPedido.Invalido,

            { ValorTotal: >= 1000m } => CategoriaPedido.AltoValor,

            { Itens.Count: >= 10 } => CategoriaPedido.AltoVolume,

            _ => CategoriaPedido.Normal
        };
    }
}
