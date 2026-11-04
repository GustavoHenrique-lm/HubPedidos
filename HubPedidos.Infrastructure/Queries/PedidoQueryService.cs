
using HubPedidos.Application.Contracts.Consultas;
using HubPedidos.Domain.Entities;
using HubPedidos.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Infrastructure.Queries;

public sealed class PedidoQueryService
{
    private readonly HubPedidosDbContext _context;

    // Consulta compilada: reutiliza a estrutura da consulta
    // e recebe os valores como parâmetros.
    private static readonly Func<
        HubPedidosDbContext,
        string?,
        bool?,
        int,
        int,
        IAsyncEnumerable<PedidoConsultaResponse>> ConsultaCompilada =
            EF.CompileAsyncQuery((
                HubPedidosDbContext context,
                string? regiao,
                bool? prioritario,
                int skip,
                int take) =>
                    context.Pedidos
                        .AsNoTracking()
                        .Where(p => regiao == null || p.Regiao == regiao)
                        .Where(p => !prioritario.HasValue ||
                                    p.Prioritario == prioritario.Value)
                        .OrderBy(p => p.Id)
                        .Skip(skip)
                        .Take(take)
                        .Select(p => new PedidoConsultaResponse(
                            p.Id.Value,
                            p.Regiao,
                            p.Prioritario,
                            p.Metadados.Canal,
                            p.Metadados.Observacao,
                            p.Itens.Sum(i => i.Quantidade * i.PrecoUnitario),
                            p.Itens.Count)));

    public PedidoQueryService(HubPedidosDbContext context)
    {
        _context = context;
    }

    // Consulta LINQ convencional.
    public async Task<(IReadOnlyList<PedidoConsultaResponse> Pedidos, int Total)>
        ConsultarAsync(PedidoConsultaRequest request)
    {
        ValidarRequest(request);

        var regiao = string.IsNullOrWhiteSpace(request.Regiao)
            ? null
            : request.Regiao.Trim().ToUpperInvariant();

        IQueryable<Pedido> consulta = _context.Pedidos
            .AsNoTracking()
            .Where(p => regiao == null || p.Regiao == regiao)
            .Where(p => !request.Prioritario.HasValue ||
                        p.Prioritario == request.Prioritario.Value);

        var total = await consulta.CountAsync().ConfigureAwait(false);

        var skip = (request.Pagina - 1) * request.TamanhoPagina;

        var pedidos = await consulta
            .OrderBy(p => p.Id)
            .Skip(skip)
            .Take(request.TamanhoPagina)
            .Select(p => new PedidoConsultaResponse(
                p.Id.Value,
                p.Regiao,
                p.Prioritario,
                p.Metadados.Canal,
                p.Metadados.Observacao,
                p.Itens.Sum(i => i.Quantidade * i.PrecoUnitario),
                p.Itens.Count))
            .ToListAsync().ConfigureAwait(false);

        return (pedidos, total);
    }

    // Consulta compilada.
    public async Task<(IReadOnlyList<PedidoConsultaResponse> Pedidos, int Total)>
        ConsultarCompiladoAsync(PedidoConsultaRequest request)
    {
        ValidarRequest(request);

        var regiao = string.IsNullOrWhiteSpace(request.Regiao)
            ? null
            : request.Regiao.Trim().ToUpperInvariant();

        var skip = (request.Pagina - 1) * request.TamanhoPagina;

        // Mantém a mesma contagem da consulta convencional.
        var total = await _context.Pedidos
            .AsNoTracking()
            .Where(p => regiao == null || p.Regiao == regiao)
            .Where(p => !request.Prioritario.HasValue ||
                        p.Prioritario == request.Prioritario.Value)
            .CountAsync().ConfigureAwait(false);

        var pedidos = new List<PedidoConsultaResponse>();

        await foreach (var pedido in ConsultaCompilada(
            _context,
            regiao,
            request.Prioritario,
            skip,
            request.TamanhoPagina).ConfigureAwait(false))
        {
            pedidos.Add(pedido);
        }

        return (pedidos, total);
    }

    private static void ValidarRequest(PedidoConsultaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Pagina < 1)
        {
            throw new ArgumentException("A página deve ser maior que zero.");
        }

        if (request.TamanhoPagina < 1 || request.TamanhoPagina > 100)
        {
            throw new ArgumentException(
                "O tamanho da página deve estar entre 1 e 100.");
        }
    }
}
