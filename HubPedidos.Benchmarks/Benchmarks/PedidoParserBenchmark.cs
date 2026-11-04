using BenchmarkDotNet.Attributes;
using HubPedidos.Processing.Parsers;

namespace HubPedidos.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class PedidoParserBenchmark
{
    private string _lote = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _lote = string.Join("\n",
            Enumerable.Range(1, 1000)
                .Select(i => $"Produto{i};2;150.50"));
    }

    [Benchmark(Baseline = true)]
    public List<PedidoImportado> Baseline()
    {
        return PedidoParser.Baseline(_lote);
    }

    [Benchmark]
    public List<PedidoImportado> Otimizado()
    {
        return PedidoParser.Otimizado(_lote);
    }
}
