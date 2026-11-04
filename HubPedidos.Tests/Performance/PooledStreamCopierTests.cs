using System.Buffers;
using HubPedidos.Processing.Streams;

namespace HubPedidos.Tests.Performance;

public class PooledStreamCopierTests
{
    [Fact]
    public async Task DeveDevolverBufferAposCopiaNormal()
    {
        var pool = new PoolDeTeste();
        using var origem = new MemoryStream(new byte[100]);
        using var destino = new MemoryStream();

        long total = await PooledStreamCopier.CopiarAsync(
            origem, destino, 100, pool: pool);

        Assert.Equal(100, total);
        Assert.Equal(1, pool.QuantidadeDevolvida);
    }

    [Fact]
    public async Task DeveDevolverBufferQuandoOcorreExcecao()
    {
        var pool = new PoolDeTeste();
        using var origem = new StreamComErro();
        using var destino = new MemoryStream();

        await Assert.ThrowsAsync<IOException>(() =>
            PooledStreamCopier.CopiarAsync(
                origem, destino, 100, pool: pool));

        Assert.Equal(1, pool.QuantidadeDevolvida);
    }

    [Fact]
    public async Task DeveDevolverBufferQuandoOperacaoEcancelada()
    {
        var pool = new PoolDeTeste();
        using var origem = new MemoryStream(new byte[100]);
        using var destino = new MemoryStream();

        using var cancelamento = new CancellationTokenSource();
        cancelamento.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            PooledStreamCopier.CopiarAsync(
                origem,
                destino,
                100,
                cancelamento.Token,
                pool));

        Assert.Equal(1, pool.QuantidadeDevolvida);
    }

    [Fact]
    public async Task DeveRejeitarConteudoAcimaDoLimiteEDevolverBuffer()
    {
        var pool = new PoolDeTeste();
        using var origem = new MemoryStream(new byte[200]);
        using var destino = new MemoryStream();

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            PooledStreamCopier.CopiarAsync(
                origem, destino, 100, pool: pool));

        Assert.Equal(1, pool.QuantidadeDevolvida);
    }

    private sealed class PoolDeTeste : ArrayPool<byte>
    {
        public int QuantidadeDevolvida { get; private set; }

        public override byte[] Rent(int minimumLength)
        {
            return new byte[minimumLength];
        }

        public override void Return(
            byte[] array,
            bool clearArray = false)
        {
            QuantidadeDevolvida++;

            if (clearArray)
            {
                Array.Clear(array);
            }
        }
    }

    private sealed class StreamComErro : MemoryStream
    {
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            throw new IOException("Erro simulado durante a leitura.");
        }
    }
}
