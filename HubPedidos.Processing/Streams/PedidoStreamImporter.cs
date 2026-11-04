using System.Text;
using HubPedidos.Processing.Parsers;

namespace HubPedidos.Processing.Streams;

public static class PedidoStreamImporter
{
    public static async Task<List<PedidoImportado>> ImportarAsync(
        Stream origem,
        long limiteBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(origem);

        if (limiteBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limiteBytes));
        }

        using var streamLimitado = new LimitedReadStream(origem, limiteBytes);

        using var leitor = new StreamReader(
            streamLimitado,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: true);

        var pedidos = new List<PedidoImportado>();

        while (await leitor.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } linha)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pedidosDaLinha = PedidoParser.Otimizado(linha);
            pedidos.AddRange(pedidosDaLinha);
        }

        return pedidos;
    }

    private sealed class LimitedReadStream : Stream
    {
        private readonly Stream _origem;
        private readonly long _limite;
        private long _totalLido;

        public LimitedReadStream(Stream origem, long limite)
        {
            _origem = origem;
            _limite = limite;
        }

        public override bool CanRead => _origem.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override long Length =>
            throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int permitido = CalcularTamanhoPermitido(count);
            int lido = _origem.Read(buffer, offset, permitido);
            RegistrarLeitura(lido);
            return lido;
        }

        public override int Read(Span<byte> buffer)
        {
            int permitido = CalcularTamanhoPermitido(buffer.Length);
            int lido = _origem.Read(buffer[..permitido]);
            RegistrarLeitura(lido);
            return lido;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            int permitido = CalcularTamanhoPermitido(buffer.Length);

            int lido = await _origem.ReadAsync(
                buffer[..permitido],
                cancellationToken).ConfigureAwait(false);

            RegistrarLeitura(lido);
            return lido;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int permitido = CalcularTamanhoPermitido(count);

            int lido = await _origem.ReadAsync(
                buffer, offset, permitido, cancellationToken).ConfigureAwait(false);

            RegistrarLeitura(lido);
            return lido;
        }

        private int CalcularTamanhoPermitido(int solicitado)
        {
            long restante = _limite - _totalLido;

            // Lê no máximo um byte além do limite para detectar excesso.
            return (int)Math.Min(solicitado, restante + 1);
        }

        private void RegistrarLeitura(int quantidade)
        {
            if (quantidade > _limite - _totalLido)
            {
                throw new InvalidDataException(
                    "O arquivo ultrapassou o limite permitido.");
            }

            _totalLido += quantidade;
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
