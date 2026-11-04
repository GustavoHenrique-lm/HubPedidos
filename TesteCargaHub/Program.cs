
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;

const string url = "http://localhost:5121/pedidos";
const int concorrencia = 4;
const int aquecimento = 5;
const int duracao = 30;

using var cliente = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(10)
};

var tempos = new ConcurrentBag<double>();
int sucessos = 0;
int erros = 0;

async Task Worker(bool medir, CancellationToken token)
{
    while (!token.IsCancellationRequested)
    {
        var relogio = Stopwatch.StartNew();

        try
        {
            using var resposta = await cliente.GetAsync(url, token);
            relogio.Stop();

            if ((int)resposta.StatusCode >= 200 &&
                (int)resposta.StatusCode < 300)
            {
                if (medir)
                {
                    Interlocked.Increment(ref sucessos);
                    tempos.Add(relogio.Elapsed.TotalMilliseconds);
                }
            }
            else if (medir)
            {
                Interlocked.Increment(ref erros);
            }
        }
        catch (OperationCanceledException)
        {
            if (medir && !token.IsCancellationRequested)
                Interlocked.Increment(ref erros);
        }
        catch
        {
            if (medir)
                Interlocked.Increment(ref erros);
        }
    }
}

Console.WriteLine("Aquecimento: 5 segundos...");

using (var cts = new CancellationTokenSource(
    TimeSpan.FromSeconds(aquecimento)))
{
    await Task.WhenAll(
        Enumerable.Range(0, concorrencia)
            .Select(_ => Worker(false, cts.Token)));
}

Console.WriteLine("Medição: 30 segundos...");

var inicio = Stopwatch.StartNew();

using (var cts = new CancellationTokenSource(
    TimeSpan.FromSeconds(duracao)))
{
    await Task.WhenAll(
        Enumerable.Range(0, concorrencia)
            .Select(_ => Worker(true, cts.Token)));
}

inicio.Stop();

var valores = tempos.OrderBy(x => x).ToArray();

double Percentil(double p)
{
    if (valores.Length == 0) return 0;

    int indice = (int)Math.Ceiling(p * valores.Length) - 1;

    return valores[Math.Clamp(indice, 0, valores.Length - 1)];
}

Console.WriteLine("\n===== RESULTADO DO TESTE =====");
Console.WriteLine($"Concorrência: {concorrencia}");
Console.WriteLine($"Duração: {inicio.Elapsed.TotalSeconds:F2} segundos");
Console.WriteLine($"Sucessos: {sucessos}");
Console.WriteLine($"Erros: {erros}");
Console.WriteLine($"Vazão: {sucessos / inicio.Elapsed.TotalSeconds:F2} req/s");
Console.WriteLine($"p50: {Percentil(0.50):F2} ms");
Console.WriteLine($"p95: {Percentil(0.95):F2} ms");
Console.WriteLine($"p99: {Percentil(0.99):F2} ms");

var arquivo = "resultado-teste-carga.csv";

File.WriteAllLines(
    arquivo,
    new[] { "Latencia_ms" }.Concat(
        valores.Select(x => x.ToString(CultureInfo.InvariantCulture))));

Console.WriteLine($"Arquivo CSV: {arquivo}");
