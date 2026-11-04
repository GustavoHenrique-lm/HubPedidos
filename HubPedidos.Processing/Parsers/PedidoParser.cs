using System.Globalization;

namespace HubPedidos.Processing.Parsers;

public record PedidoImportado(
    string Produto,
    int Quantidade,
    decimal Preco);

public static class PedidoParser
{
    // Versão inicial: utiliza Split e gera strings.
    public static List<PedidoImportado> Baseline(string texto)
    {
        var pedidos = new List<PedidoImportado>();

        foreach (var linha in texto.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(linha))
            {
                continue;
            }

            var partes = linha.Trim().Split(';');

            if (partes.Length != 3)
            {
                throw new FormatException("Linha inválida.");
            }

            pedidos.Add(new PedidoImportado(
                partes[0],
                int.Parse(partes[1]),
                decimal.Parse(partes[2], CultureInfo.InvariantCulture)
            ));
        }

        return pedidos;
    }

    // Versão otimizada: utiliza ReadOnlySpan.
    public static List<PedidoImportado> Otimizado(string texto)
    {
        var pedidos = new List<PedidoImportado>();
        ReadOnlySpan<char> restante = texto.AsSpan();

        while (!restante.IsEmpty)
        {
            int quebra = restante.IndexOf('\n');

            ReadOnlySpan<char> linha = quebra >= 0
                ? restante[..quebra]
                : restante;

            restante = quebra >= 0
                ? restante[(quebra + 1)..]
                : ReadOnlySpan<char>.Empty;

            linha = linha.Trim();

            if (linha.IsEmpty)
            {
                continue;
            }

            int primeiro = linha.IndexOf(';');

            if (primeiro < 0)
            {
                throw new FormatException("Linha inválida.");
            }

            int segundoRelativo =
                linha[(primeiro + 1)..].IndexOf(';');

            if (segundoRelativo < 0)
            {
                throw new FormatException("Linha inválida.");
            }

            int segundo = primeiro + 1 + segundoRelativo;

            if (linha[(segundo + 1)..].Contains(';'))
            {
                throw new FormatException("Linha inválida.");
            }

            string produto = linha[..primeiro].ToString();

            int quantidade = int.Parse(
                linha[(primeiro + 1)..segundo]);

            decimal preco = decimal.Parse(
                linha[(segundo + 1)..],
                CultureInfo.InvariantCulture);

            pedidos.Add(new PedidoImportado(
                produto,
                quantidade,
                preco));
        }

        return pedidos;
    }
}
