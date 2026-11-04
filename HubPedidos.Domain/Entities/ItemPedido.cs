namespace HubPedidos.Domain.Entities;

public sealed record ItemPedido
{
    private ItemPedido()
    {
        Produto = string.Empty;
    }

    public string Produto { get; private set; }

    public int Quantidade { get; private set; }

    public decimal PrecoUnitario { get; private set; }

    public decimal Subtotal => Quantidade * PrecoUnitario;

    public ItemPedido(string produto, int quantidade, decimal precoUnitario)
    {
        if (string.IsNullOrWhiteSpace(produto))
        {
            throw new ArgumentException("O nome do produto é obrigatório.");
        }

        if (quantidade <= 0)
        {
            throw new ArgumentException("A quantidade deve ser maior que zero.");
        }

        if (precoUnitario <= 0)
        {
            throw new ArgumentException("O preço deve ser maior que zero.");
        }

        Produto = produto;
        Quantidade = quantidade;
        PrecoUnitario = precoUnitario;
    }
}
