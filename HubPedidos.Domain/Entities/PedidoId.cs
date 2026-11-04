namespace HubPedidos.Domain.Entities;

public readonly record struct PedidoId
{
    public Guid Value { get; }

    private PedidoId(Guid value)
    {
        Value = value;
    }

    public static PedidoId Criar(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("O identificador do pedido é inválido.");
        }

        return new PedidoId(value);
    }
}
