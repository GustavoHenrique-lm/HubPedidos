
using HubPedidos.Domain.ValueObjects;

namespace HubPedidos.Domain.Entities;

public sealed class Pedido
{
    private List<ItemPedido> _itens = [];

    private Pedido()
    {
        Regiao = string.Empty;
        Metadados = new MetadadosPedido("Sistema", string.Empty);
    }

    public PedidoId Id { get; private set; }

    public IReadOnlyList<ItemPedido> Itens => _itens.AsReadOnly();

    public string Regiao { get; private set; }

    public bool Prioritario { get; private set; }

    public MetadadosPedido Metadados { get; private set; }

    public Guid? UsuarioId { get; private set; }

    public long Versao { get; private set; }

    public decimal ValorTotal => _itens.Sum(item => item.Subtotal);

    public Pedido(
        PedidoId id,
        IEnumerable<ItemPedido> itens,
        string regiao,
        bool prioritario,
        MetadadosPedido? metadados = null)
    {
        ArgumentNullException.ThrowIfNull(itens);

        var listaItens = itens.ToList();

        if (listaItens.Count == 0)
        {
            throw new ArgumentException(
                "O pedido deve possuir pelo menos um item.");
        }

        if (string.IsNullOrWhiteSpace(regiao))
        {
            throw new ArgumentException("A região é obrigatória.");
        }

        _itens.AddRange(listaItens);

        Id = id;
        Regiao = regiao.Trim().ToUpperInvariant();
        Prioritario = prioritario;
        Metadados = metadados ??
            new MetadadosPedido("Sistema", string.Empty);

        Versao = 1;
    }

    public void AssociarUsuario(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador do usuário é inválido.");
        }

        UsuarioId = usuarioId;
    }

    public void AlterarPrioridade(bool prioritario)
    {
        if (Prioritario == prioritario)
        {
            return;
        }

        Prioritario = prioritario;
        Versao++;
    }
}
