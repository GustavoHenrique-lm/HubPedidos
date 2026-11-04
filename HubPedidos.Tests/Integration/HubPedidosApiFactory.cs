
using System.Text;
using HubPedidos.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace HubPedidos.Tests.Integration;

public class HubPedidosApiFactory : WebApplicationFactory<Program>
{
    private const string ChaveJwtTeste =
        "ChaveDeTesteSeguraComMaisDeTrintaEDoisCaracteres123!";

    private readonly Dictionary<string, string?> _valoresAnteriores = new();

    private SqliteConnection? _connection;

    public HubPedidosApiFactory()
    {
        DefinirVariavel("DOTNET_ENVIRONMENT", "Testing");
        DefinirVariavel("ASPNETCORE_ENVIRONMENT", "Testing");
        DefinirVariavel("Jwt__Key", ChaveJwtTeste);
        DefinirVariavel("Jwt__Issuer", "HubPedidos.Api");
        DefinirVariavel("Jwt__Audience", "HubPedidos.Client");
        DefinirVariavel("Jwt__ExpirationMinutes", "60");
    }

    private void DefinirVariavel(string nome, string valor)
    {
        _valoresAnteriores[nome] =
            Environment.GetEnvironmentVariable(nome);

        Environment.SetEnvironmentVariable(nome, valor);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<HubPedidosDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<HubPedidosDbContext>>();
            services.RemoveAll<HubPedidosDbContext>();

            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddDbContext<HubPedidosDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(ChaveJwtTeste));

                    options.TokenValidationParameters.ValidIssuer =
                        "HubPedidos.Api";

                    options.TokenValidationParameters.ValidAudience =
                        "HubPedidos.Client";

                    options.TokenValidationParameters.ValidateIssuer = true;
                    options.TokenValidationParameters.ValidateAudience = true;
                    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    options.TokenValidationParameters.ValidateLifetime = true;
                });

            services.RemoveAll<IHostedService>();
        });
    }

    public async Task InicializarBancoAsync()
    {
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<HubPedidosDbContext>();

        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();

            foreach (var variavel in _valoresAnteriores)
            {
                Environment.SetEnvironmentVariable(
                    variavel.Key,
                    variavel.Value);
            }
        }
    }
}
