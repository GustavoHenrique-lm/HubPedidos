namespace HubPedidos.Application.Contracts.Consultas;

public sealed record PedidoConsultaResponse(
    Guid Id,
    string Regiao,
    bool Prioritario,
    string Canal,
    string Observacao,
    decimal ValorTotal,
    int QuantidadeItens);
