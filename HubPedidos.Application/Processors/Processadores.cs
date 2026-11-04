using HubPedidos.Domain.Entities;
using HubPedidos.Domain.Enums;

namespace HubPedidos.Application.Processors;

[ProcessadorPedido(CategoriaPedido.Normal, 1)]
public class ProcessadorNormal : IProcessadorPedido
{
    public string Processar(Pedido pedido)
    {
        return "Pedido normal processado com sucesso.";
    }
}

[ProcessadorPedido(CategoriaPedido.Prioritario, 1)]
public class ProcessadorPrioritario : IProcessadorPedido
{
    public string Processar(Pedido pedido)
    {
        return "Pedido prioritário processado com prioridade.";
    }
}
