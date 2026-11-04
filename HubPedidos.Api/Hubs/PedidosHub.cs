
using System.Security.Claims;
using HubPedidos.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Api.Hubs;

[Authorize]
public sealed class PedidosHub : Hub<IPedidosClient>
{
    private readonly HubPedidosDbContext _context;

    public PedidosHub(HubPedidosDbContext context)
    {
        _context = context;
    }

    public async Task AcompanharPedido(Guid pedidoId)
    {
        if (pedidoId == Guid.Empty)
        {
            throw new HubException("Identificador do pedido inválido.");
        }

        var usuarioIdTexto = Context.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdTexto, out var usuarioId))
        {
            throw new HubException("Usuário não autenticado.");
        }

        // Confere no banco se o pedido pertence ao usuário.
        var autorizado = await _context.Pedidos
            .AsNoTracking()
            .AnyAsync(p =>
                p.Id.Value == pedidoId &&
                p.UsuarioId == usuarioId).ConfigureAwait(false);

        if (!autorizado)
        {
            throw new HubException(
                "Você não tem autorização para acompanhar este pedido.");
        }

        var grupo = PedidoGrupo.Nome(pedidoId, usuarioId);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            grupo).ConfigureAwait(false);
    }

    public async Task PararAcompanharPedido(Guid pedidoId)
    {
        var usuarioIdTexto = Context.User?
            .FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdTexto, out var usuarioId))
        {
            throw new HubException("Usuário não autenticado.");
        }

        var grupo = PedidoGrupo.Nome(pedidoId, usuarioId);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            grupo).ConfigureAwait(false);
    }
}
