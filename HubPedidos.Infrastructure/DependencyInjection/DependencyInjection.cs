
using HubPedidos.Infrastructure.Data;
using HubPedidos.Infrastructure.Identity;
using HubPedidos.Infrastructure.Queries;
using HubPedidos.Infrastructure.Queue;
using HubPedidos.Infrastructure.Services;
using HubPedidos.Notifications.Contracts;
using HubPedidos.Notifications.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HubPedidos.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddHubPedidosInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuração do banco de dados.
        var connectionString =
            configuration.GetConnectionString("HubPedidos");

        services.AddDbContext<HubPedidosDbContext>(options =>
        {
            options.UseSqlServer(connectionString);

            options.LogTo(
                Console.WriteLine,
                new[] { DbLoggerCategory.Database.Command.Name },
                LogLevel.Information);
        });

        // Configuração do ASP.NET Core Identity.
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<IdentityRole<Guid>>()
           .AddEntityFrameworkStores<HubPedidosDbContext>();

        // Configuração da fila.
        var queueOptions = new PedidoQueueOptions
        {
            Capacity = int.TryParse(
                configuration["PedidoQueue:Capacity"],
                out var capacity)
                    ? capacity : 100,

            Consumers = int.TryParse(
                configuration["PedidoQueue:Consumers"],
                out var consumers)
                    ? consumers : 4,

            DatabaseConcurrencyLimit = int.TryParse(
                configuration["PedidoQueue:DatabaseConcurrencyLimit"],
                out var concurrency)
                    ? concurrency : 2
        };

        services.AddSingleton(queueOptions);

        // Registro dos serviços de consulta.
        services.AddScoped<PedidoQueryService>();
        services.AddScoped<TrackingDemo>();
        services.AddScoped<TrackingUpdateDemo>();

        // Serviço de processamento.
        services.AddScoped<
            HubPedidos.Application.Contracts.IProcessarPedidoService,
            ProcessarPedidoService>();

        // Serviços de notificações.
        services.AddScoped<
            INotificationService,
            NotificationService>();

        // Serviços da fila.
        services.AddSingleton<PedidoQueue>();
        services.AddSingleton<PedidoQueueMetrics>();
        services.AddSingleton<PedidoConcurrencyLimiter>();

        // Worker em segundo plano.
        services.AddHostedService<PedidoWorker>();

        return services;
    }
}
