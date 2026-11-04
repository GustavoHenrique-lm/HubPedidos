
using System.Text;
using HubPedidos.Api.Authentication;
using HubPedidos.Api.Hubs;
using HubPedidos.Application.Contracts;
using HubPedidos.Application.Contracts.Consultas;
using HubPedidos.Application.Contracts.V2;
using HubPedidos.Application.Mappings;
using HubPedidos.Application.Processing;
using HubPedidos.Application.Processors;
using HubPedidos.Domain.Services;
using HubPedidos.Infrastructure.Data;
using HubPedidos.Infrastructure.DependencyInjection;
using HubPedidos.Infrastructure.Queries;
using HubPedidos.Infrastructure.Queue;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Validação das dependências.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Registro da infraestrutura.
builder.Services.AddHubPedidosInfrastructure(
    builder.Configuration);

// Registro dos processadores da aplicação.
builder.Services.AddSingleton(
    new CatalogoProcessadores(typeof(ProcessadorNormal).Assembly));

// ======================================================
// CONFIGURAÇÃO JWT
// ======================================================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<JwtTokenService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Chave JWT não configurada.");

var jwtSettings = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "Configurações JWT não encontradas.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

        // Permite receber o JWT na conexão SignalR.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/pedidos"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ======================================================
// CONFIGURAÇÃO OUTPUT CACHE
// ======================================================

builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("ResumoPedidosPublico", policy =>
    {
        policy
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByQuery("*")
            .Tag("resumo-pedidos");
    });
});

// ======================================================
// CONFIGURAÇÃO SIGNALR
// ======================================================

builder.Services.AddSignalR();

var app = builder.Build();

app.UseStaticFiles();

// ======================================================
// MIDDLEWARE DE AUTENTICAÇÃO E AUTORIZAÇÃO
// ======================================================

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.MapControllers();

// ======================================================
// HUB SIGNALR
// ======================================================

app.MapHub<PedidosHub>("/hubs/pedidos");

// ======================================================
// ROTAS LEGADAS - MANTIDAS PARA COMPATIBILIDADE
// ======================================================

// Cadastro de pedidos.
app.MapPost("/pedidos", async (
    CriarPedidoRequest request,
    CatalogoProcessadores catalogo,
    HubPedidosDbContext context,
    IOutputCacheStore cacheStore,
    CancellationToken cancellationToken) =>
{
    try
    {
        var pedido = PedidoMapper.ParaDominio(request);
        var categoria = ClassificadorPedido.Classificar(pedido);

        if (categoria == HubPedidos.Domain.Enums.CategoriaPedido.Invalido)
        {
            return Results.BadRequest("Pedido inválido.");
        }

        var processador = catalogo.Obter(categoria);
        var mensagem = processador.Processar(pedido);

        await context.Pedidos.AddAsync(pedido, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cacheStore.EvictByTagAsync(
            "resumo-pedidos",
            cancellationToken).ConfigureAwait(false);

        var response = PedidoMapper.ParaResponse(pedido, categoria);

        return Results.Ok(response with { Mensagem = mensagem });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// Consulta de pedidos.
app.MapGet("/pedidos", async (
    [AsParameters] PedidoConsultaRequest request,
    PedidoQueryService service) =>
{
    try
    {
        var resultado = await service.ConsultarAsync(request).ConfigureAwait(false);

        return Results.Ok(new
        {
            resultado.Total,
            resultado.Pedidos
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// ======================================================
// API V1 - CONTRATO ORIGINAL
// ======================================================

// Cadastro de pedidos V1.
app.MapPost("/api/v1/pedidos", async (
    CriarPedidoRequest request,
    CatalogoProcessadores catalogo,
    HubPedidosDbContext context,
    IOutputCacheStore cacheStore,
    CancellationToken cancellationToken) =>
{
    try
    {
        var pedido = PedidoMapper.ParaDominio(request);
        var categoria = ClassificadorPedido.Classificar(pedido);

        if (categoria == HubPedidos.Domain.Enums.CategoriaPedido.Invalido)
        {
            return Results.BadRequest("Pedido inválido.");
        }

        var processador = catalogo.Obter(categoria);
        var mensagem = processador.Processar(pedido);

        await context.Pedidos.AddAsync(pedido, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cacheStore.EvictByTagAsync(
            "resumo-pedidos",
            cancellationToken).ConfigureAwait(false);

        var response = PedidoMapper.ParaResponse(pedido, categoria);

        return Results.Ok(response with { Mensagem = mensagem });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// Consulta V1.
app.MapGet("/api/v1/pedidos", async (
    [AsParameters] PedidoConsultaRequest request,
    PedidoQueryService service) =>
{
    try
    {
        var resultado = await service.ConsultarAsync(request).ConfigureAwait(false);

        return Results.Ok(new
        {
            resultado.Total,
            resultado.Pedidos
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// ======================================================
// API V2 - NOVO CONTRATO
// ======================================================

// Cadastro de pedidos V2.
app.MapPost("/api/v2/pedidos", async (
    CriarPedidoV2Request request,
    CatalogoProcessadores catalogo,
    HubPedidosDbContext context,
    IOutputCacheStore cacheStore,
    CancellationToken cancellationToken) =>
{
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
        var categoria = ClassificadorPedido.Classificar(pedido);

        if (categoria == HubPedidos.Domain.Enums.CategoriaPedido.Invalido)
        {
            return Results.BadRequest("Pedido inválido.");
        }

        var processador = catalogo.Obter(categoria);
        var mensagem = processador.Processar(pedido);

        await context.Pedidos.AddAsync(pedido, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await cacheStore.EvictByTagAsync(
            "resumo-pedidos",
            cancellationToken).ConfigureAwait(false);

        var response = new PedidoV2Response(
            pedido.Id.Value,
            categoria,
            pedido.ValorTotal,
            mensagem
        );

        return Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// Consulta V2.
app.MapGet("/api/v2/pedidos", async (
    [AsParameters] PedidoConsultaRequest request,
    PedidoQueryService service) =>
{
    try
    {
        var resultado = await service.ConsultarAsync(request).ConfigureAwait(false);

        return Results.Ok(new
        {
            resultado.Total,
            Pedidos = resultado.Pedidos.Select(pedido => new
            {
                PedidoId = pedido.Id,
                pedido.Regiao,
                pedido.Prioritario,
                pedido.Canal,
                pedido.Observacao,
                pedido.ValorTotal,
                pedido.QuantidadeItens
            })
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// ======================================================
// DEMAIS ENDPOINTS ORIGINAIS
// ======================================================

// Consulta compilada.
app.MapGet("/pedidos/compilado", async (
    [AsParameters] PedidoConsultaRequest request,
    PedidoQueryService service) =>
{
    try
    {
        var resultado = await service.ConsultarCompiladoAsync(request).ConfigureAwait(false);

        return Results.Ok(new
        {
            resultado.Total,
            resultado.Pedidos
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// Demonstração de Tracking e AsNoTracking.
app.MapGet("/pedidos/{id:guid}/tracking", async (
    Guid id,
    TrackingDemo demo) =>
{
    var resultado = await demo.CompararAsync(id).ConfigureAwait(false);

    return Results.Ok(resultado);
});

// Atualização de pedido usando Tracking.
app.MapPut("/pedidos/{id:guid}/tracking-update", async (
    Guid id,
    TrackingUpdateDemo demo) =>
{
    var resultado = await demo.AtualizarAsync(id).ConfigureAwait(false);

    return Results.Ok(resultado);
});

// ======================================================
// FILA DE PROCESSAMENTO
// ======================================================

// Envio com backpressure (Wait).
app.MapPost("/pedidos/fila", async (
    Guid pedidoId,
    PedidoQueue queue,
    PedidoQueueMetrics metrics,
    HttpContext httpContext) =>
{
    if (pedidoId == Guid.Empty)
    {
        return Results.BadRequest("Identificador inválido.");
    }

    metrics.RegistrarRecebido();

    var job = new ProcessarPedidoJob(
        pedidoId,
        DateTimeOffset.UtcNow);

    try
    {
        await queue.WriteAsync(
            job,
            httpContext.RequestAborted).ConfigureAwait(false);

        metrics.RegistrarAceito(pedidoId);

        return Results.Accepted(
            $"/pedidos/fila/{pedidoId}",
            new
            {
                Mensagem = "Pedido enviado para processamento.",
                PedidoId = pedidoId
            });
    }
    catch (InvalidOperationException)
    {
        return Results.Conflict(
            "Este pedido já foi enviado para processamento.");
    }
    catch (OperationCanceledException)
        when (httpContext.RequestAborted.IsCancellationRequested)
    {
        return Results.StatusCode(499);
    }
});

// Envio com tentativa imediata.
app.MapPost("/pedidos/fila/tentar", (
    Guid pedidoId,
    PedidoQueue queue,
    PedidoQueueMetrics metrics) =>
{
    if (pedidoId == Guid.Empty)
    {
        return Results.BadRequest("Identificador inválido.");
    }

    metrics.RegistrarRecebido();

    var job = new ProcessarPedidoJob(
        pedidoId,
        DateTimeOffset.UtcNow);

    var aceito = queue.TryWrite(job);

    if (!aceito)
    {
        if (queue.PedidoJaRegistrado(pedidoId))
        {
            return Results.Conflict(
                "Este pedido já foi enviado para processamento.");
        }

        metrics.RegistrarRejeitado();

        return Results.Json(
            new
            {
                Mensagem = "Fila cheia. Tente novamente mais tarde.",
                PedidoId = pedidoId
            },
            statusCode: StatusCodes.Status429TooManyRequests);
    }

    metrics.RegistrarAceito(pedidoId);

    return Results.Accepted(
        $"/pedidos/fila/{pedidoId}",
        new
        {
            Mensagem = "Pedido enviado para processamento.",
            PedidoId = pedidoId
        });
});

// ======================================================
// MÉTRICAS
// ======================================================

// Métricas da fila.
app.MapGet("/pedidos/fila/metricas", (
    PedidoQueueMetrics metrics,
    PedidoQueue queue) =>
{
    return Results.Ok(
        metrics.ObterResumo(queue.TotalDescartados));
});

// Métricas de concorrência.
app.MapGet("/pedidos/fila/concorrencia", (
    PedidoConcurrencyLimiter limiter) =>
{
    return Results.Ok(limiter.ObterMetricas());
});

app.Run();

public partial class Program { }
