using System.Buffers;

namespace HubPedidos.Processing.Streams;

public static class PooledStreamCopier
{
    public static async Task<long> CopiarAsync(
        Stream origem,
        Stream destino,
        long limiteBytes,
        CancellationToken cancellationToken = default,
        ArrayPool<byte>? pool = null)
    {
        ArgumentNullException.ThrowIfNull(origem);
        ArgumentNullException.ThrowIfNull(destino);

        if (limiteBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limiteBytes));
        }

        pool ??= ArrayPool<byte>.Shared;

        const int tamanhoBuffer = 8192;

        byte[] buffer = pool.Rent(tamanhoBuffer);
        long totalBytes = 0;

        try
        {
            int quantidadeLida;

            while ((quantidadeLida = await origem.ReadAsync(
                buffer.AsMemory(0, tamanhoBuffer),
                cancellationToken).ConfigureAwait(false)) > 0)
            {
                totalBytes += quantidadeLida;

                if (totalBytes > limiteBytes)
                {
                    throw new InvalidDataException(
                        "O arquivo ultrapassou o limite permitido.");
                }

                await destino.WriteAsync(
                    buffer.AsMemory(0, quantidadeLida),
                    cancellationToken).ConfigureAwait(false);
            }

            await destino.FlushAsync(cancellationToken).ConfigureAwait(false);

            return totalBytes;
        }
        finally
        {
            pool.Return(buffer, clearArray: true);
        }
    }
}
