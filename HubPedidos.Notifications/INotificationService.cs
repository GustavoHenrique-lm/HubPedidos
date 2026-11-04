
namespace HubPedidos.Notifications.Contracts;

public interface INotificationService
{
    public Task EnviarAsync(
        Guid pedidoId,
        string mensagem,
        CancellationToken cancellationToken);
}
