
using System.Security.Claims;
using HubPedidos.Application.Contracts;
using HubPedidos.Application.Contracts.V2;
using HubPedidos.Application.Mappings;
using HubPedidos.Application.Processors;
using HubPedidos.Domain.Services;
using HubPedidos.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Api.Controllers;

[ApiController]
[Route("api/v3/pedidos")]
[Authorize]
public class PedidosV3Controller : ControllerBase
{
    private readonly CatalogoProcessadores _catalogo;
    private readonly HubPedidosDbContext _context;
    private readonly IOutputCacheStore _cacheStore;

    public PedidosV3Controller(
        CatalogoProcessadores catalogo,
        HubPedidosDbContext context,
        IOutputCacheStore cacheStore)
    {
        _catalogo = catalogo;
        _context = context;
        _cacheStore = cacheStore;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CriarPedidoV2Request request)
    {
        var usuarioIdTexto = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdTexto, out var usuarioId))
        {
            return Unauthorized("Usuário não identificado.");
        }

        try
        {
            var requestV1 = new CriarPedidoRequest(
                request.PedidoId,
                request.Itens.Select(item =>
                    new ItemPedidoRequest(
                        item.Produto,
                        item.Quantidade,
                        item.PrecoUnitario
                    )).ToList(),
                request.Regiao,
                request.Prioritario,
                request.Canal,
                request.Observacao
            );

            var pedido = PedidoMapper.ParaDominio(requestV1);

            pedido.AssociarUsuario(usuarioId);

            var categoria = ClassificadorPedido.Classificar(pedido);

            if (categoria ==
                HubPedidos.Domain.Enums.CategoriaPedido.Invalido)
            {
                return BadRequest("Pedido inválido.");
            }

            var processador = _catalogo.Obter(categoria);
            var mensagem = processador.Processar(pedido);

            await _context.Pedidos.AddAsync(pedido).ConfigureAwait(false);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            // Invalida o resumo após o cadastro persistido.
            await _cacheStore.EvictByTagAsync(
                "resumo-pedidos",
                HttpContext.RequestAborted).ConfigureAwait(false);

            var response = new PedidoV2Response(
                pedido.Id.Value,
                categoria,
                pedido.ValorTotal,
                mensagem
            );

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (DbUpdateException)
        {
            return Conflict(
                "Não foi possível cadastrar o pedido.");
        }
    }
}
