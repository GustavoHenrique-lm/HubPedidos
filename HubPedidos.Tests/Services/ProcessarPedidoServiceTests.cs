
using HubPedidos.Application.Processing;
using HubPedidos.Domain.Entities;
using HubPedidos.Infrastructure.Data;
using HubPedidos.Infrastructure.Services;
using HubPedidos.Notifications.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Tests.Services;

public class ProcessarPedidoServiceTests
{
    [Fact]
    public async Task DeveEnviarNotificacaoQuandoPedidoExistir()
    {
        // Arrange
        await using var context = CriarContexto();

        var pedido = CriarPedido();

        context.Pedidos.Add(pedido);
        await context.SaveChangesAsync();

        var notificacao = new NotificacaoSimulada();

        var service = new ProcessarPedidoService(
            context,
            notificacao);

        var job = new ProcessarPedidoJob(
            pedido.Id.Value,
            DateTimeOffset.UtcNow);

        // Act
        await service.ProcessarAsync(
            job,
            CancellationToken.None);

        // Assert
        Assert.Equal(1, notificacao.QuantidadeEnvios);
        Assert.Equal(pedido.Id.Value, notificacao.UltimoPedidoId);

        Assert.Equal(
            "Pedido processado com sucesso.",
            notificacao.UltimaMensagem);
    }

    [Fact]
    public async Task NaoDeveEnviarNotificacaoQuandoPedidoNaoExistir()
    {
        // Arrange
        await using var context = CriarContexto();

        var notificacao = new NotificacaoSimulada();

        var service = new ProcessarPedidoService(
            context,
            notificacao);

        var job = new ProcessarPedidoJob(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProcessarAsync(
                job,
                CancellationToken.None));

        // Assert
        Assert.Equal(0, notificacao.QuantidadeEnvios);
    }

    [Fact]
    public async Task DeveRespeitarCancelamentoDaOperacao()
    {
        // Arrange
        await using var context = CriarContexto();

        var notificacao = new NotificacaoSimulada();

        var service = new ProcessarPedidoService(
            context,
            notificacao);

        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        var job = new ProcessarPedidoJob(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ProcessarAsync(
                job,
                cancellation.Token));

        // Assert
        Assert.Equal(0, notificacao.QuantidadeEnvios);
    }

    private static HubPedidosDbContext CriarContexto()
    {
        var options =
            new DbContextOptionsBuilder<HubPedidosDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

        var context = new HubPedidosDbContext(options);

        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }

    private static Pedido CriarPedido()
    {
        var item = new ItemPedido(
            "Notebook",
            1,
            2500m);

        return new Pedido(
            PedidoId.Criar(Guid.NewGuid()),
            new List<ItemPedido> { item },
            "SP",
            false);
    }

    private sealed class NotificacaoSimulada : INotificationService
    {
        public int QuantidadeEnvios { get; private set; }

        public Guid UltimoPedidoId { get; private set; }

        public string? UltimaMensagem { get; private set; }

        public Task EnviarAsync(
            Guid pedidoId,
            string mensagem,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            QuantidadeEnvios++;
            UltimoPedidoId = pedidoId;
            UltimaMensagem = mensagem;

            return Task.CompletedTask;
        }
    }
}
