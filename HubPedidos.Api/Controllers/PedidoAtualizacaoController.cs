
using System.Security.Claims;
using HubPedidos.Api.Hubs;
using HubPedidos.Domain.Entities;
using HubPedidos.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Api.Controllers;

[ApiController]
[Route("api/v3/pedidos")]
[Authorize]
public class PedidoAtualizacaoController : ControllerBase
{
    private readonly HubPedidosDbContext _context;
    private readonly IHubContext<PedidosHub, IPedidosClient> _hubContext;
    private readonly IOutputCacheStore _cacheStore;

    public PedidoAtualizacaoController(
        HubPedidosDbContext context,
        IHubContext<PedidosHub, IPedidosClient> hubContext,
        IOutputCacheStore cacheStore)
    {
        _context = context;
        _hubContext = hubContext;
        _cacheStore = cacheStore;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Consultar(Guid id)
    {
        var usuarioIdTexto = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdTexto, out var usuarioId))
        {
            return Unauthorized("Usuário não identificado.");
        }

        var pedidoId = PedidoId.Criar(id);

        var pedido = await _context.Pedidos
            .AsNoTracking()
            .Where(p =>
                p.Id == pedidoId &&
                p.UsuarioId == usuarioId)
            .Select(p => new
            {
                PedidoId = p.Id.Value,
                p.Prioritario,
                p.Versao
            })
            .FirstOrDefaultAsync().ConfigureAwait(false);

        if (pedido is null)
        {
            return NotFound(
                "Pedido não encontrado ou acesso não autorizado.");
        }

        var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(
            pedido,
            new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web));

        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(json))
            .ToLowerInvariant();

        var etag = $"\"{hash}\"";

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "private, no-cache";

        var etagsRecebidos = Request.Headers.IfNoneMatch
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries);

        var etagCorrespondente = etagsRecebidos.Any(valor =>
        {
            var recebido = valor.Trim();

            if (recebido == "*")
            {
                return true;
            }

            if (recebido.StartsWith(
                "W/", StringComparison.OrdinalIgnoreCase))
            {
                recebido = recebido[2..].Trim();
            }

            return string.Equals(
                recebido,
                etag,
                StringComparison.Ordinal);
        });

        if (etagCorrespondente)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return File(json, "application/json");
    }

    [HttpPut("{id:guid}/prioridade")]
    public async Task<IActionResult> AlterarPrioridade(
        Guid id,
        [FromQuery] bool prioritario)
    {
        var usuarioIdTexto = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdTexto, out var usuarioId))
        {
            return Unauthorized("Usuário não identificado.");
        }

        var pedidoId = PedidoId.Criar(id);

        var pedido = await _context.Pedidos
            .FirstOrDefaultAsync(p =>
                p.Id == pedidoId &&
                p.UsuarioId == usuarioId).ConfigureAwait(false);

        if (pedido is null)
        {
            return NotFound(
                "Pedido não encontrado ou acesso não autorizado.");
        }

        var versaoAnterior = pedido.Versao;

        pedido.AlterarPrioridade(prioritario);

        await _context.SaveChangesAsync().ConfigureAwait(false);

        if (pedido.Versao > versaoAnterior)
        {
            // Invalida o resumo armazenado em cache.
            await _cacheStore.EvictByTagAsync(
                "resumo-pedidos",
                HttpContext.RequestAborted).ConfigureAwait(false);

            var evento = new PedidoAtualizadoEvent(
                pedido.Id.Value,
                pedido.Versao,
                DateTimeOffset.UtcNow,
                pedido.Prioritario
            );

            var grupo = PedidoGrupo.Nome(id, usuarioId);

            await _hubContext.Clients
                .Group(grupo)
                .PedidoAtualizado(evento).ConfigureAwait(false);
        }

        return Ok(new
        {
            Mensagem = "Pedido atualizado com sucesso.",
            PedidoId = pedido.Id.Value,
            pedido.Prioritario,
            pedido.Versao
        });
    }
}
