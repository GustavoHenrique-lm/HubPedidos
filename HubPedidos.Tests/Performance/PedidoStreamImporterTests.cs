using System.Text;
using HubPedidos.Processing.Streams;

namespace HubPedidos.Tests.Performance;

public class PedidoStreamImporterTests
{
    [Fact]
    public async Task DeveImportarPedidosEmUtf8()
    {
        byte[] dados = Encoding.UTF8.GetBytes(
            "Café;2;15.50\r\nNotebook;1;3500.00");

        using var origem = new MemoryStream(dados);

        var pedidos = await PedidoStreamImporter.ImportarAsync(
            origem, 1024);

        Assert.Equal(2, pedidos.Count);
        Assert.Equal("Café", pedidos[0].Produto);
        Assert.Equal("Notebook", pedidos[1].Produto);
    }

    [Fact]
    public async Task DeveAceitarArquivoVazio()
    {
        using var origem = new MemoryStream();

        var pedidos = await PedidoStreamImporter.ImportarAsync(
            origem, 1024);

        Assert.Empty(pedidos);
    }

    [Fact]
    public async Task DeveRejeitarLinhaIncompleta()
    {
        byte[] dados = Encoding.UTF8.GetBytes("Notebook;2");

        using var origem = new MemoryStream(dados);

        await Assert.ThrowsAsync<FormatException>(() =>
            PedidoStreamImporter.ImportarAsync(origem, 1024));
    }

    [Fact]
    public async Task DeveRejeitarArquivoAcimaDoLimite()
    {
        byte[] dados = Encoding.UTF8.GetBytes(
            "Notebook;2;3500.00\nMouse;1;80.00");

        using var origem = new MemoryStream(dados);

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            PedidoStreamImporter.ImportarAsync(origem, 10));
    }

    [Fact]
    public async Task DeveProcessarFluxoGrandeSemArmazenarTodoConteudo()
    {
        using var origem = new FluxoGerado(10_000);

        var pedidos = await PedidoStreamImporter.ImportarAsync(
            origem, 200_000);

        Assert.Equal(10_000, pedidos.Count);
        Assert.Equal("Produto", pedidos[0].Produto);
    }

    private sealed class FluxoGerado : Stream
    {
        private readonly byte[] _linha =
            Encoding.UTF8.GetBytes("Produto;1;2\n");

        private readonly int _quantidadeLinhas;
        private int _linhasEnviadas;
        private int _posicaoNaLinha;

        public FluxoGerado(int quantidadeLinhas)
        {
            _quantidadeLinhas = quantidadeLinhas;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        public override int Read(
            byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int total = 0;

            while (total < buffer.Length &&
                   _linhasEnviadas < _quantidadeLinhas)
            {
                int restante = _linha.Length - _posicaoNaLinha;
                int copiar = Math.Min(
                    restante, buffer.Length - total);

                _linha.AsMemory(_posicaoNaLinha, copiar)
                    .CopyTo(buffer[total..]);

                total += copiar;
                _posicaoNaLinha += copiar;

                if (_posicaoNaLinha == _linha.Length)
                {
                    _posicaoNaLinha = 0;
                    _linhasEnviadas++;
                }
            }

            return ValueTask.FromResult(total);
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Length =>
            throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}
