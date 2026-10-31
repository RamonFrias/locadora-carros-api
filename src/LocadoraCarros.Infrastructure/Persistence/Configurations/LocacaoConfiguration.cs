using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraCarros.Infrastructure.Persistence.Configurations;

internal sealed class LocacaoConfiguration : IEntityTypeConfiguration<Locacao>
{
    public void Configure(EntityTypeBuilder<Locacao> builder)
    {
        builder.ToTable("Locacoes");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.ReservadaPorPerfil).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Protecao).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.FormaPagamento).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.StatusPagamento).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.StatusCaucao).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.MotivoCancelamento).HasMaxLength(500);
        builder.Property(l => l.Observacoes).HasMaxLength(2000);

        foreach (var valor in new[]
                 {
                     nameof(Locacao.ValorDiaria), nameof(Locacao.ValorDiarias), nameof(Locacao.ValorProtecao),
                     nameof(Locacao.ValorAdicionais), nameof(Locacao.TaxaDevolucaoOutraLoja), nameof(Locacao.ValorTotalPrevisto),
                     nameof(Locacao.ValorDiariasExtras), nameof(Locacao.TaxaCombustivel), nameof(Locacao.ValorDanos),
                     nameof(Locacao.ValorPago), nameof(Locacao.ValorReembolsado), nameof(Locacao.ValorCaucao),
                     nameof(Locacao.ValorCaucaoRetido)
                 })
            builder.Property<decimal>(valor).HasPrecision(12, 2);

        builder.OwnsMany(l => l.Adicionais, a =>
        {
            a.ToTable("LocacaoAdicionais");
            a.WithOwner().HasForeignKey("LocacaoId");
            a.Property<int>("Id");
            a.HasKey("Id");
            a.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30);
            a.Property(x => x.ValorDiario).HasPrecision(10, 2);
        });
        builder.Navigation(l => l.Adicionais).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(l => l.Codigo).IsUnique();
        builder.HasIndex(l => new { l.CarroId, l.Status });
        builder.HasIndex(l => new { l.LojaRetiradaId, l.DataRetiradaPrevista });
        builder.HasIndex(l => new { l.LojaDevolucaoId, l.DataDevolucaoPrevista });

        builder.HasOne<Cliente>().WithMany().HasForeignKey(l => l.ClienteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(l => l.ReservadaPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Carro>().WithMany().HasForeignKey(l => l.CarroId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Loja>().WithMany().HasForeignKey(l => l.LojaRetiradaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Loja>().WithMany().HasForeignKey(l => l.LojaDevolucaoId).OnDelete(DeleteBehavior.Restrict);
    }
}
