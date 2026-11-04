namespace HubPedidos.Domain.ValueObjects;

public sealed record MetadadosPedido
{
    public string Canal { get; }
    public string Observacao { get; }

    public MetadadosPedido(string canal, string observacao)
    {
        if (string.IsNullOrWhiteSpace(canal))
        {
            throw new ArgumentException("O canal é obrigatório.");
        }

        Canal = canal.Trim();
        Observacao = observacao?.Trim() ?? string.Empty;
    }
}
