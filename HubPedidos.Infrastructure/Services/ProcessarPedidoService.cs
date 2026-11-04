
using HubPedidos.Application.Contracts;
using HubPedidos.Application.Processing;
using HubPedidos.Domain.Entities;
using HubPedidos.Infrastructure.Data;
using HubPedidos.Notifications.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Infrastructure.Services;

public sealed class ProcessarPedidoService : IProcessarPedidoService
{
    private readonly HubPedidosDbContext _context;
    private readonly INotificationService _notificationService;

    public ProcessarPedidoService(
        HubPedidosDbContext context,
        INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task ProcessarAsync(
        ProcessarPedidoJob job,
        CancellationToken cancellationToken)
    {
        var pedidoId = PedidoId.Criar(job.PedidoId);

        var pedidoExiste = await _context.Pedidos
            .AsNoTracking()
            .AnyAsync(
                p => p.Id == pedidoId,
                cancellationToken).ConfigureAwait(false);

        if (!pedidoExiste)
        {
            throw new InvalidOperationException(
                $"Pedido {job.PedidoId} não encontrado.");
        }

        // Envia a notificação após confirmar a existência do pedido.
        await _notificationService.EnviarAsync(
            job.PedidoId,
            "Pedido processado com sucesso.",
            cancellationToken).ConfigureAwait(false);
    }
}
