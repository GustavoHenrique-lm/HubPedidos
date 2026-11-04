using HubPedidos.Domain.Enums;

namespace HubPedidos.Application.Processors;

[AttributeUsage(AttributeTargets.Class)]
public class ProcessadorPedidoAttribute : Attribute
{
    public CategoriaPedido Categoria { get; }
    public int Versao { get; }

    public ProcessadorPedidoAttribute(CategoriaPedido categoria, int versao)
    {
        Categoria = categoria;
        Versao = versao;
    }
}
