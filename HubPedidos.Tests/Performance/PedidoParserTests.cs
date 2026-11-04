using HubPedidos.Processing.Parsers;

namespace HubPedidos.Tests.Performance;

public class PedidoParserTests
{
    [Fact]
    public void BaselineEOtimizadoDevemGerarResultadosIguais()
    {
        var entrada =
            "Notebook;2;3500.50\n" +
            "Mouse;3;80.00\n" +
            "Teclado;1;150.00";

        var baseline = PedidoParser.Baseline(entrada);
        var otimizado = PedidoParser.Otimizado(entrada);

        Assert.Equal(baseline, otimizado);
    }

    [Fact]
    public void DeveAceitarEntradaVazia()
    {
        Assert.Empty(PedidoParser.Baseline(""));
        Assert.Empty(PedidoParser.Otimizado(""));
    }

    [Fact]
    public void DeveRejeitarLinhaIncompleta()
    {
        Assert.Throws<FormatException>(
            () => PedidoParser.Baseline("Notebook;2"));

        Assert.Throws<FormatException>(
            () => PedidoParser.Otimizado("Notebook;2"));
    }

    [Fact]
    public void DeveAceitarProdutoComUtf8Multibyte()
    {
        var entrada = "Café;2;15.50\n";

        var baseline = PedidoParser.Baseline(entrada);
        var otimizado = PedidoParser.Otimizado(entrada);

        Assert.Equal(baseline, otimizado);
        Assert.Equal("Café", otimizado[0].Produto);
    }

    [Fact]
    public void DeveRejeitarQuantidadeInvalida()
    {
        Assert.Throws<FormatException>(
            () => PedidoParser.Otimizado("Notebook;abc;100"));
    }
}
