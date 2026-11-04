using HubPedidos.Domain.Entities;

namespace HubPedidos.Application.Processors;

public interface IProcessadorPedido
{
    public string Processar(Pedido pedido);
}
