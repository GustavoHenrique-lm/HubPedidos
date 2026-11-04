namespace HubPedidos.Application.Contracts.Consultas;

public sealed record PedidoConsultaRequest(
    string? Regiao,
    bool? Prioritario,
    int Pagina = 1,
    int TamanhoPagina = 10);
