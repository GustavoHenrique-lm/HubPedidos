using HubPedidos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HubPedidos.Infrastructure.Configurations;

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        var pedidoIdConverter = new ValueConverter<PedidoId, Guid>(
            id => id.Value,
            value => PedidoId.Criar(value));

        builder.ToTable("Pedidos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(pedidoIdConverter)
            .HasColumnName("PedidoId")
            .ValueGeneratedNever();

        builder.Property(p => p.Regiao)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Prioritario)
            .IsRequired();

        builder.Ignore(p => p.ValorTotal);

        builder.HasIndex(p => p.Regiao);

        builder.ComplexProperty(p => p.Metadados, metadata =>
        {
            metadata.Property(m => m.Canal)
                .HasColumnName("Canal")
                .HasMaxLength(80)
                .IsRequired();

            metadata.Property(m => m.Observacao)
                .HasColumnName("Observacao")
                .HasMaxLength(500)
                .IsRequired();
        });

        builder.OwnsMany(p => p.Itens, item =>
        {
            item.ToTable("ItensPedido");

            item.WithOwner()
                .HasForeignKey("PedidoId");

            item.Property<int>("ItemId");

            item.HasKey("ItemId");

            item.Property(i => i.Produto)
                .HasMaxLength(150)
                .IsRequired();

            item.Property(i => i.Quantidade)
                .IsRequired();

            item.Property(i => i.PrecoUnitario)
                .HasPrecision(18, 2)
                .IsRequired();

            item.Ignore(i => i.Subtotal);
        });

        builder.Navigation(p => p.Itens)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
