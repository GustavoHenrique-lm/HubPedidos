namespace HubPedidos.Infrastructure.Queue;

public sealed class PedidoConcurrencyLimiter
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _limite;

    private int _operacoesAtivas;
    private int _maximoObservado;

    public PedidoConcurrencyLimiter(PedidoQueueOptions options)
    {
        _limite = options.DatabaseConcurrencyLimit;
        _semaphore = new SemaphoreSlim(_limite, _limite);
    }

    public async Task AguardarPermissaoAsync(
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        var ativas = Interlocked.Increment(ref _operacoesAtivas);

        int atual;

        do
        {
            atual = Volatile.Read(ref _maximoObservado);

            if (ativas <= atual)
            {
                break;
            }
        } while (Interlocked.CompareExchange(
            ref _maximoObservado,
            ativas,
            atual) != atual);
    }

    public void Liberar()
    {
        Interlocked.Decrement(ref _operacoesAtivas);
        _semaphore.Release();
    }

    public object ObterMetricas()
    {
        return new
        {
            LimiteConfigurado = _limite,
            OperacoesAtivas = Volatile.Read(ref _operacoesAtivas),
            MaximoObservado = Volatile.Read(ref _maximoObservado)
        };
    }
}
