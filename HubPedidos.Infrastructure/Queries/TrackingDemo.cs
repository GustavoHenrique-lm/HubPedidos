
using HubPedidos.Domain.Entities;
using HubPedidos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Infrastructure.Queries;

public sealed class TrackingDemo
{
    private readonly HubPedidosDbContext _context;

    public TrackingDemo(HubPedidosDbContext context)
    {
        _context = context;
    }

    public async Task<string> CompararAsync(Guid id)
    {
        var pedidoId = PedidoId.Criar(id);

        var rastreado = await _context.Pedidos
            .FirstOrDefaultAsync(p => p.Id == pedidoId).ConfigureAwait(false);

        var quantidadeRastreados = _context.ChangeTracker
            .Entries<Pedido>()
            .Count();

        _context.ChangeTracker.Clear();

        var semRastreamento = await _context.Pedidos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pedidoId).ConfigureAwait(false);

        var quantidadeSemRastreamento = _context.ChangeTracker
            .Entries<Pedido>()
            .Count();

        return $"Tracking: {quantidadeRastreados}; " +
               $"AsNoTracking: {quantidadeSemRastreamento}";
    }
}
