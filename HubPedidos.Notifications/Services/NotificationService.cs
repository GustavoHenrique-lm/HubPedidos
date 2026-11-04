
using HubPedidos.Notifications.Contracts;
using Microsoft.Extensions.Logging;

namespace HubPedidos.Notifications.Services;

public sealed class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task EnviarAsync(
        Guid pedidoId,
        string mensagem,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Notificação do pedido {PedidoId}: {Mensagem}",
            pedidoId,
            mensagem);

        return Task.CompletedTask;
    }
}
