
using System.Collections.Concurrent;
using System.Diagnostics;

namespace HubPedidos.Infrastructure.Queue;

public sealed class PedidoQueueMetrics
{
    private long _recebidos;
    private long _aceitos;
    private long _rejeitados;
    private long _processados;
    private long _falhas;
    private long _aguardando;
    private long _emProcessamento;

    private long _tempoEsperaTotalTicks;
    private long _tempoProcessamentoTotalTicks;

    private readonly long _inicioMonitoramento =
        Stopwatch.GetTimestamp();

    private readonly ConcurrentDictionary<Guid, string> _estados = new();

    private readonly ConcurrentDictionary<Guid, long>
        _inicioProcessamento = new();

    public void RegistrarRecebido()
    {
        Interlocked.Increment(ref _recebidos);
    }

    public void RegistrarAceito(Guid id)
    {
        Interlocked.Increment(ref _aceitos);
        Interlocked.Increment(ref _aguardando);

        _estados[id] = "Na fila";
    }

    public void RegistrarRejeitado()
    {
        Interlocked.Increment(ref _rejeitados);
    }

    public void RegistrarInicio(Guid id)
    {
        Interlocked.Decrement(ref _aguardando);
        Interlocked.Increment(ref _emProcessamento);

        _inicioProcessamento[id] = Stopwatch.GetTimestamp();

        _estados[id] = "Em processamento";
    }

    public void RegistrarProcessado(
        Guid id,
        TimeSpan tempoEspera)
    {
        Interlocked.Increment(ref _processados);
        Interlocked.Decrement(ref _emProcessamento);

        Interlocked.Add(
            ref _tempoEsperaTotalTicks,
            tempoEspera.Ticks);

        RegistrarLatencia(id);

        _estados[id] = "Processado";
    }

    public void RegistrarFalha(Guid id)
    {
        Interlocked.Increment(ref _falhas);
        Interlocked.Decrement(ref _emProcessamento);

        RegistrarLatencia(id);

        _estados[id] = "Falha";
    }

    private void RegistrarLatencia(Guid id)
    {
        if (_inicioProcessamento.TryRemove(
            id,
            out var inicio))
        {
            var duracao = Stopwatch.GetElapsedTime(inicio);

            Interlocked.Add(
                ref _tempoProcessamentoTotalTicks,
                duracao.Ticks);
        }
    }

    public object ObterResumo(long descartados = 0)
    {
        var recebidos = Interlocked.Read(ref _recebidos);
        var aceitos = Interlocked.Read(ref _aceitos);
        var rejeitados = Interlocked.Read(ref _rejeitados);
        var processados = Interlocked.Read(ref _processados);
        var falhas = Interlocked.Read(ref _falhas);

        var aguardando = Interlocked.Read(ref _aguardando);
        var emProcessamento =
            Interlocked.Read(ref _emProcessamento);

        var esperaTicks =
            Interlocked.Read(ref _tempoEsperaTotalTicks);

        var processamentoTicks =
            Interlocked.Read(ref _tempoProcessamentoTotalTicks);

        var concluidos = processados + falhas;

        var tempoMonitoramento =
            Stopwatch.GetElapsedTime(_inicioMonitoramento);

        var tempoMedioEsperaMs = processados == 0
            ? 0
            : TimeSpan.FromTicks(esperaTicks)
                .TotalMilliseconds / processados;

        var latenciaMediaMs = concluidos == 0
            ? 0
            : TimeSpan.FromTicks(processamentoTicks)
                .TotalMilliseconds / concluidos;

        var throughput = tempoMonitoramento.TotalSeconds <= 0
            ? 0
            : processados / tempoMonitoramento.TotalSeconds;

        return new
        {
            Recebidos = recebidos,
            Aceitos = aceitos,
            Rejeitados = rejeitados,
            Descartados = descartados,

            Aguardando = aguardando,
            EmProcessamento = emProcessamento,
            ProfundidadeFila = aguardando,

            Processados = processados,
            Falhas = falhas,
            Concluidos = concluidos,

            TempoMedioEsperaMs = tempoMedioEsperaMs,
            LatenciaMediaMs = latenciaMediaMs,

            ThroughputPedidosPorSegundo = throughput,

            TempoMonitoramentoSegundos =
                tempoMonitoramento.TotalSeconds,

            Estados = _estados.ToArray()
        };
    }
}
