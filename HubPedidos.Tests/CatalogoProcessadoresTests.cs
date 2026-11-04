using HubPedidos.Application.Processors;
using HubPedidos.Domain.Enums;

namespace HubPedidos.Tests;

public class CatalogoProcessadoresTests
{
    [Fact]
    public void DeveEncontrarProcessadoresNaAssemblyInformada()
    {
        var catalogo = new CatalogoProcessadores(
            typeof(ProcessadorNormal).Assembly);

        Assert.IsType<ProcessadorNormal>(
            catalogo.Obter(CategoriaPedido.Normal));

        Assert.IsType<ProcessadorPrioritario>(
            catalogo.Obter(CategoriaPedido.Prioritario));
    }

    [Fact]
    public void DeveRejeitarCategoriaSemProcessador()
    {
        var catalogo = new CatalogoProcessadores(
            typeof(ProcessadorNormal).Assembly);

        Assert.Throws<InvalidOperationException>(
            () => catalogo.Obter(CategoriaPedido.AltoValor));
    }
    [ProcessadorPedido(CategoriaPedido.Normal, 1)]
    public class ProcessadorDuplicado : IProcessadorPedido
    {
        public string Processar(HubPedidos.Domain.Entities.Pedido pedido)
        {
            return "Processado.";
        }
    }

    [Fact]
    public void DeveRejeitarProcessadoresDuplicados()
    {
        Assert.Throws<InvalidOperationException>(
            () => new CatalogoProcessadores(
                typeof(ProcessadorDuplicado).Assembly));
    }
    [ProcessadorPedido(CategoriaPedido.Normal, 1)]
    public class SegundoProcessadorDuplicado : IProcessadorPedido
    {
        public string Processar(HubPedidos.Domain.Entities.Pedido pedido)
        {
            return "Segundo processador.";
        }
    }
}
