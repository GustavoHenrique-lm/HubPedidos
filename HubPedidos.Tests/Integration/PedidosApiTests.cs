
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace HubPedidos.Tests.Integration;

public class PedidosApiTests
{
    [Fact]
    public async Task ApiDeveIniciarEConsultarPedidos()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/pedidos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Versao1DeveManterContratoOriginal()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        using var client = factory.CreateClient();

        var pedido = new
        {
            id = Guid.NewGuid(),
            itens = new[]
            {
                new
                {
                    produto = "Notebook",
                    quantidade = 1,
                    precoUnitario = 2500m
                }
            },
            regiao = "SP",
            prioritario = false,
            canal = "Sistema",
            observacao = "Teste V1"
        };

        var response = await client.PostAsJsonAsync(
            "/api/v1/pedidos", pedido);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.TryGetProperty("id", out _));
        Assert.True(document.RootElement.TryGetProperty("categoria", out _));
        Assert.True(document.RootElement.TryGetProperty("valorTotal", out _));
        Assert.True(document.RootElement.TryGetProperty("mensagem", out _));
    }

    [Fact]
    public async Task Versao2DeveUtilizarNovoContrato()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        using var client = factory.CreateClient();

        var pedido = new
        {
            pedidoId = Guid.NewGuid(),
            itens = new[]
            {
                new
                {
                    produto = "Monitor",
                    quantidade = 1,
                    precoUnitario = 1200m
                }
            },
            regiao = "SP",
            prioritario = false,
            canal = "Sistema",
            observacao = "Teste V2"
        };

        var response = await client.PostAsJsonAsync(
            "/api/v2/pedidos", pedido);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.TryGetProperty("pedidoId", out _));
        Assert.True(document.RootElement.TryGetProperty("classificacao", out _));
        Assert.True(document.RootElement.TryGetProperty("valorTotal", out _));
        Assert.True(document.RootElement.TryGetProperty("mensagem", out _));

        Assert.False(document.RootElement.TryGetProperty("id", out _));
        Assert.False(document.RootElement.TryGetProperty("categoria", out _));
    }

    [Fact]
    public async Task VersaoNaoSuportadaDeveRetornar404()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v4/pedidos");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task VersoesV1EV2DevemCoexistir()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        using var client = factory.CreateClient();

        var respostaV1 = await client.GetAsync("/api/v1/pedidos");
        var respostaV2 = await client.GetAsync("/api/v2/pedidos");

        Assert.Equal(HttpStatusCode.OK, respostaV1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, respostaV2.StatusCode);
    }

    [Fact]
    public async Task Versao1DevePreservarValoresDoPedido()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        using var client = factory.CreateClient();

        var pedido = new
        {
            id = Guid.NewGuid(),
            itens = new[]
            {
                new
                {
                    produto = "Notebook",
                    quantidade = 2,
                    precoUnitario = 2500m
                }
            },
            regiao = "SP",
            prioritario = false,
            canal = "Sistema",
            observacao = "Teste de compatibilidade"
        };

        var response = await client.PostAsJsonAsync(
            "/api/v1/pedidos", pedido);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        Assert.Equal(pedido.id, root.GetProperty("id").GetGuid());
        Assert.Equal(5000m, root.GetProperty("valorTotal").GetDecimal());
    }

    [Fact]
    public async Task ETagDeveRetornar304QuandoRepresentacaoNaoMudar()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        var (client, pedidoId) =
            await CriarClienteAutenticadoComPedidoAsync(factory);

        using (client)
        {
            var primeiraResposta = await client.GetAsync(
                $"/api/v3/pedidos/{pedidoId}");

            Assert.Equal(HttpStatusCode.OK, primeiraResposta.StatusCode);

            var etag = primeiraResposta.Headers.ETag?.ToString();
            Assert.False(string.IsNullOrWhiteSpace(etag));

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v3/pedidos/{pedidoId}");

            request.Headers.TryAddWithoutValidation(
                "If-None-Match", etag);

            var segundaResposta = await client.SendAsync(request);

            Assert.Equal(
                HttpStatusCode.NotModified,
                segundaResposta.StatusCode);

            var corpo = await segundaResposta.Content.ReadAsStringAsync();
            Assert.Empty(corpo);
        }
    }

    [Fact]
    public async Task ETagDeveMudarQuandoPedidoForAlterado()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        var (client, pedidoId) =
            await CriarClienteAutenticadoComPedidoAsync(factory);

        using (client)
        {
            var primeiraResposta = await client.GetAsync(
                $"/api/v3/pedidos/{pedidoId}");

            var etagAnterior =
                primeiraResposta.Headers.ETag?.ToString();

            Assert.Equal(HttpStatusCode.OK, primeiraResposta.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(etagAnterior));

            var atualizacao = await client.PutAsync(
                $"/api/v3/pedidos/{pedidoId}/prioridade?prioritario=true",
                null);

            Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v3/pedidos/{pedidoId}");

            request.Headers.TryAddWithoutValidation(
                "If-None-Match", etagAnterior);

            var respostaAtualizada = await client.SendAsync(request);

            Assert.Equal(
                HttpStatusCode.OK,
                respostaAtualizada.StatusCode);

            var etagNovo =
                respostaAtualizada.Headers.ETag?.ToString();

            Assert.NotEqual(etagAnterior, etagNovo);
        }
    }

    [Fact]
    public async Task ETagInvalidoDeveRetornarRepresentacaoAtual()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        var (client, pedidoId) =
            await CriarClienteAutenticadoComPedidoAsync(factory);

        using (client)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/v3/pedidos/{pedidoId}");

            request.Headers.TryAddWithoutValidation(
                "If-None-Match", "\"etag-invalido\"");

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var corpo = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(corpo));

            Assert.NotNull(response.Headers.ETag);
        }
    }

    // Testa a invalidação após a criação de um pedido.
    [Fact]
    public async Task CacheDeveSerInvalidadoQuandoPedidoForCriado()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        using var client = factory.CreateClient();

        var primeiraConsulta = await client.GetAsync(
            "/api/v3/pedidos/resumo");

        Assert.Equal(HttpStatusCode.OK, primeiraConsulta.StatusCode);

        var resumoInicial = await primeiraConsulta.Content.ReadAsStringAsync();
        using var documentoInicial = JsonDocument.Parse(resumoInicial);

        Assert.Empty(documentoInicial.RootElement.EnumerateArray());

        var pedido = new
        {
            id = Guid.NewGuid(),
            itens = new[]
            {
                new
                {
                    produto = "Notebook",
                    quantidade = 1,
                    precoUnitario = 2500m
                }
            },
            regiao = "SP",
            prioritario = false,
            canal = "Sistema",
            observacao = "Teste de invalidação do cache"
        };

        var criacao = await client.PostAsJsonAsync(
            "/api/v1/pedidos", pedido);

        Assert.Equal(HttpStatusCode.OK, criacao.StatusCode);

        var segundaConsulta = await client.GetAsync(
            "/api/v3/pedidos/resumo");

        Assert.Equal(HttpStatusCode.OK, segundaConsulta.StatusCode);

        var resumoAtualizado = await segundaConsulta.Content.ReadAsStringAsync();
        using var documentoAtualizado = JsonDocument.Parse(resumoAtualizado);

        Assert.Equal(
            1,
            documentoAtualizado.RootElement[0]
                .GetProperty("quantidade")
                .GetInt32());
    }

    // Testa a invalidação após alterar a prioridade.
    [Fact]
    public async Task CacheDeveSerInvalidadoQuandoPrioridadeForAlterada()
    {
        using var factory = new HubPedidosApiFactory();
        await factory.InicializarBancoAsync();

        var (client, pedidoId) =
            await CriarClienteAutenticadoComPedidoAsync(factory);

        using (client)
        {
            var primeiraConsulta = await client.GetAsync(
                "/api/v3/pedidos/resumo");

            Assert.Equal(HttpStatusCode.OK, primeiraConsulta.StatusCode);

            var resumoInicial = await primeiraConsulta.Content.ReadAsStringAsync();
            using var documentoInicial = JsonDocument.Parse(resumoInicial);

            Assert.False(
                documentoInicial.RootElement[0]
                    .GetProperty("prioritario")
                    .GetBoolean());

            var atualizacao = await client.PutAsync(
                $"/api/v3/pedidos/{pedidoId}/prioridade?prioritario=true",
                null);

            Assert.Equal(HttpStatusCode.OK, atualizacao.StatusCode);

            var segundaConsulta = await client.GetAsync(
                "/api/v3/pedidos/resumo");

            Assert.Equal(HttpStatusCode.OK, segundaConsulta.StatusCode);

            var resumoAtualizado = await segundaConsulta.Content.ReadAsStringAsync();
            using var documentoAtualizado = JsonDocument.Parse(resumoAtualizado);

            Assert.True(
                documentoAtualizado.RootElement[0]
                    .GetProperty("prioritario")
                    .GetBoolean());
        }
    }

    private static async Task<(HttpClient Client, Guid PedidoId)>
        CriarClienteAutenticadoComPedidoAsync(
            HubPedidosApiFactory factory)
    {
        var client = factory.CreateClient();

        var email = $"teste{Guid.NewGuid():N}@exemplo.com";
        const string senha = "Teste123!";

        var cadastro = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                email,
                senha
            }).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, cadastro.StatusCode);

        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email,
                senha
            }).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var loginJson = await login.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var documentoLogin = JsonDocument.Parse(loginJson);

        var token = documentoLogin.RootElement
            .GetProperty("token")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var pedidoId = Guid.NewGuid();

        var pedido = new
        {
            pedidoId,
            itens = new[]
            {
                new
                {
                    produto = "Notebook",
                    quantidade = 1,
                    precoUnitario = 2500m
                }
            },
            regiao = "SP",
            prioritario = false,
            canal = "Sistema",
            observacao = "Pedido para teste de ETag"
        };

        var criacao = await client.PostAsJsonAsync(
            "/api/v3/pedidos", pedido).ConfigureAwait(false);

        var mensagemErro = await criacao.Content.ReadAsStringAsync().ConfigureAwait(false);
        var detalheAutenticacao =
            criacao.Headers.WwwAuthenticate.ToString();

        Assert.True(
            criacao.StatusCode == HttpStatusCode.OK,
            $"Erro ao criar pedido. " +
            $"Status: {(int)criacao.StatusCode}. " +
            $"Resposta: {mensagemErro}. " +
            $"Autenticação: {detalheAutenticacao}");

        return (client, pedidoId);
    }
}
