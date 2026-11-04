using HubPedidos.Domain.Entities;
using HubPedidos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Infrastructure.Queries;

public sealed class TrackingUpdateDemo
{
    private readonly HubPedidosDbContext _context;

    public TrackingUpdateDemo(HubPedidosDbContext context)
    {
        _context = context;
    }

    public async Task<string> AtualizarAsync(Guid id)
    {
        var pedidoId = PedidoId.Criar(id);

        // Consulta com tracking no DbContext atual.
        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(p => p.Id == pedidoId).ConfigureAwait(false);

        if (pedido is null)
        {
            return "Pedido não encontrado.";
        }

        var prioridadeAnterior = pedido.Prioritario;

        // Altera a entidade rastreada.
        pedido.AlterarPrioridade(!prioridadeAnterior);

        // Persiste a alteração.
        await _context.SaveChangesAsync().ConfigureAwait(false);

        // Confirma a alteração no mesmo contexto.
        return $"Prioridade anterior: {prioridadeAnterior}; " +
               $"Nova prioridade: {pedido.Prioritario}; " +
               $"Estado após salvar: {_context.Entry(pedido).State}";
    }
}
