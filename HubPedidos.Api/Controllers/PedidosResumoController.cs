
using HubPedidos.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Api.Controllers;

[ApiController]
[Route("api/v3/pedidos/resumo")]
public class PedidosResumoController : ControllerBase
{
    private readonly HubPedidosDbContext _context;

    public PedidosResumoController(
        HubPedidosDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [OutputCache(PolicyName = "ResumoPedidosPublico")]
    public async Task<IActionResult> Consultar()
    {
        var resumo = await _context.Pedidos
            .AsNoTracking()
            .GroupBy(p => new
            {
                p.Regiao,
                p.Prioritario
            })
            .Select(grupo => new
            {
                Regiao = grupo.Key.Regiao,
                Prioritario = grupo.Key.Prioritario,
                Quantidade = grupo.Count()
            })
            .ToListAsync().ConfigureAwait(false);

        return Ok(resumo);
    }
}
