
using System.Security.Claims;
using HubPedidos.Api.Hubs;
using HubPedidos.Domain.Entities;
using HubPedidos.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HubPedidos.Tests.Integration;

public class PedidosHubTests
{
    [Fact]
    public async Task AcompanharPedido_UsuarioProprietario_DeveAdicionarAoGrupo()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var pedidoId = PedidoId.Criar(Guid.NewGuid());

        await using var context = CriarContexto();
        await context.Database.EnsureCreatedAsync();

        var pedido = CriarPedido(pedidoId);
        pedido.AssociarUsuario(usuarioId);

        context.Pedidos.Add(pedido);
        await context.SaveChangesAsync();

        var grupos = new Mock<IGroupManager>();

        var hub = CriarHub(context, grupos, usuarioId);

        // Act
        await hub.AcompanharPedido(pedidoId.Value);

        // Assert
        grupos.Verify(
            x => x.AddToGroupAsync(
                "conexao-teste",
                PedidoGrupo.Nome(pedidoId.Value, usuarioId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AcompanharPedido_UsuarioNaoProprietario_DeveNegarAcesso()
    {
        // Arrange
        var proprietarioId = Guid.NewGuid();
        var outroUsuarioId = Guid.NewGuid();
        var pedidoId = PedidoId.Criar(Guid.NewGuid());

        await using var context = CriarContexto();
        await context.Database.EnsureCreatedAsync();

        var pedido = CriarPedido(pedidoId);
        pedido.AssociarUsuario(proprietarioId);

        context.Pedidos.Add(pedido);
        await context.SaveChangesAsync();

        var grupos = new Mock<IGroupManager>();

        var hub = CriarHub(context, grupos, outroUsuarioId);

        // Act
        var excecao = await Assert.ThrowsAsync<HubException>(
            () => hub.AcompanharPedido(pedidoId.Value));

        // Assert
        Assert.Contains("autorização", excecao.Message);

        grupos.Verify(
            x => x.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AcompanharPedido_AposPerderAcesso_DeveRevalidarAutorizacao()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var pedidoId = PedidoId.Criar(Guid.NewGuid());

        await using var context = CriarContexto();
        await context.Database.EnsureCreatedAsync();

        var pedido = CriarPedido(pedidoId);
        pedido.AssociarUsuario(usuarioId);

        context.Pedidos.Add(pedido);
        await context.SaveChangesAsync();

        var grupos = new Mock<IGroupManager>();
        var hub = CriarHub(context, grupos, usuarioId);

        // Primeira inscrição: acesso permitido.
        await hub.AcompanharPedido(pedidoId.Value);

        // Simula perda da autorização antes da reconexão.
        pedido.AssociarUsuario(Guid.NewGuid());
        await context.SaveChangesAsync();

        // Act: nova inscrição após a reconexão.
        var excecao = await Assert.ThrowsAsync<HubException>(
            () => hub.AcompanharPedido(pedidoId.Value));

        // Assert
        Assert.Contains("autorização", excecao.Message);

        grupos.Verify(
            x => x.AddToGroupAsync(
                "conexao-teste",
                PedidoGrupo.Nome(pedidoId.Value, usuarioId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static HubPedidosDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<HubPedidosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new HubPedidosDbContext(options);
    }

    private static Pedido CriarPedido(PedidoId pedidoId)
    {
        var item = new ItemPedido(
            "Produto de teste",
            1,
            10m);

        return new Pedido(
            pedidoId,
            new[] { item },
            "SP",
            false);
    }

    private static PedidosHub CriarHub(
        HubPedidosDbContext context,
        Mock<IGroupManager> grupos,
        Guid usuarioId)
    {
        var hub = new PedidosHub(context);

        var usuario = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        usuarioId.ToString())
                },
                "Teste"));

        var contexto = new Mock<HubCallerContext>();
        contexto.SetupGet(x => x.User).Returns(usuario);
        contexto.SetupGet(x => x.ConnectionId).Returns("conexao-teste");

        hub.Context = contexto.Object;
        hub.Groups = grupos.Object;

        return hub;
    }
}

