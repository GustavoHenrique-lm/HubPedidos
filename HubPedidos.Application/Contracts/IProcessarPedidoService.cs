
using HubPedidos.Application.Processing;

namespace HubPedidos.Application.Contracts;

public interface IProcessarPedidoService
{
    public Task ProcessarAsync(
        ProcessarPedidoJob job,
        CancellationToken cancellationToken);
}
