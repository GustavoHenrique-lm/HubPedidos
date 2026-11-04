
using HubPedidos.Domain.Entities;
using HubPedidos.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HubPedidos.Infrastructure.Data;

public class HubPedidosDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public HubPedidosDbContext(
        DbContextOptions<HubPedidosDbContext> options)
        : base(options)
    {
    }

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(HubPedidosDbContext).Assembly);
    }
}
