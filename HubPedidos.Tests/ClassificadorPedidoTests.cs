using HubPedidos.Application.Contracts;
using HubPedidos.Application.Mappings;
using HubPedidos.Domain.Entities;
using HubPedidos.Domain.Enums;
using HubPedidos.Domain.Services;

namespace HubPedidos.Tests;

public class ClassificadorPedidoTests
{
    private Pedido CriarPedido(
        decimal preco,
        int quantidade = 1,
        string regiao = "RJ",
        bool prioritario = false)
    {
        return new Pedido(
            PedidoId.Criar(Guid.NewGuid()),
            new[] { new ItemPedido("Produto", quantidade, preco) },
            regiao,
            prioritario
        );
    }

    [Fact]
    public void DeveClassificarPedidoNormal()
    {
        var pedido = CriarPedido(100);

        Assert.Equal(
            CategoriaPedido.Normal,
            ClassificadorPedido.Classificar(pedido));
    }

    [Fact]
    public void DeveClassificarPedidoPrioritario()
    {
        var pedido = CriarPedido(100, prioritario: true);

        Assert.Equal(
            CategoriaPedido.Prioritario,
            ClassificadorPedido.Classificar(pedido));
    }

    [Fact]
    public void DeveClassificarPedidoDeAltoValor()
    {
        var pedido = CriarPedido(1000);

        Assert.Equal(
            CategoriaPedido.AltoValor,
            ClassificadorPedido.Classificar(pedido));
    }

    [Fact]
    public void DeveClassificarPedidoDeAltoVolume()
    {
        var itens = Enumerable.Range(1, 10)
            .Select(i => new ItemPedido($"Produto {i}", 1, 10))
            .ToList();

        var pedido = new Pedido(
            PedidoId.Criar(Guid.NewGuid()),
            itens,
            "RJ",
            false
        );

        Assert.Equal(
            CategoriaPedido.AltoVolume,
            ClassificadorPedido.Classificar(pedido));
    }
    [Fact]
    public void DeveClassificarPedidoNuloComoInvalido()
    {
        Assert.Equal(
            CategoriaPedido.Invalido,
            ClassificadorPedido.Classificar(null));
    }

    [Fact]
    public void DevePriorizarPedidoDeAltoValorDaRegiaoSP()
    {
        var pedido = CriarPedido(1000, regiao: "SP");

        Assert.Equal(
            CategoriaPedido.Prioritario,
            ClassificadorPedido.Classificar(pedido));
    }

    [Fact]
    public void PedidoIdNaoDeveAceitarGuidVazio()
    {
        Assert.Throws<ArgumentException>(
            () => PedidoId.Criar(Guid.Empty));
    }

    [Fact]
    public void ItemPedidoDeveSerImutavel()
    {
        var item = new ItemPedido("Notebook", 1, 1500);

        var copia = item with { };

        Assert.Equal(item.Produto, copia.Produto);
        Assert.Equal(item.Quantidade, copia.Quantidade);
        Assert.Equal(item.PrecoUnitario, copia.PrecoUnitario);
    }

    [Fact]
    public void DeveRejeitarQuantidadeInvalida()
    {
        Assert.Throws<ArgumentException>(
            () => new ItemPedido("Notebook", 0, 1500));
    }
    [Fact]
    public void DeveRejeitarDtoInvalidoAntesDeCriarPedido()
    {
        var request = new CriarPedidoRequest(
            Guid.Empty,
            new List<ItemPedidoRequest>
            {
            new("Notebook", 0, 1500)
            },
            "SP",
            false
        );

        Assert.Throws<ArgumentException>(
            () => PedidoMapper.ParaDominio(request));
    }
}
