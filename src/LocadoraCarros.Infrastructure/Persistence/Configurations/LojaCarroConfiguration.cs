using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraCarros.Infrastructure.Persistence.Configurations;

internal sealed class LojaConfiguration : IEntityTypeConfiguration<Loja>
{
    public void Configure(EntityTypeBuilder<Loja> builder)
    {
        builder.ToTable("Lojas");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Nome).HasMaxLength(150).IsRequired();
        builder.Property(l => l.Cnpj).HasConversion(v => v.Valor, v => Cnpj.Criar(v)).HasMaxLength(14).IsRequired();
        builder.Property(l => l.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Telefone).HasConversion(v => v.Valor, v => Telefone.Criar(v)).HasMaxLength(11);
        builder.Property(l => l.Email).HasConversion(v => v.Valor, v => Email.Criar(v)).HasMaxLength(254);
        builder.ComplexProperty(l => l.Endereco, e => e.MapearEndereco());

        builder.HasIndex(l => l.Codigo).IsUnique();
        builder.HasIndex(l => l.Cnpj).IsUnique();
    }
}

internal sealed class CarroConfiguration : IEntityTypeConfiguration<Carro>
{
    public void Configure(EntityTypeBuilder<Carro> builder)
    {
        builder.ToTable("Carros");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Placa).HasConversion(v => v.Valor, v => Placa.Criar(v)).HasMaxLength(7).IsRequired();
        builder.Property(c => c.PlacaChave).HasMaxLength(7).IsRequired();
        builder.Property(c => c.Renavam).HasMaxLength(11).IsRequired();
        builder.Property(c => c.Chassi).HasMaxLength(17).IsRequired();
        builder.Property(c => c.Marca).HasMaxLength(60).IsRequired();
        builder.Property(c => c.Modelo).HasMaxLength(60).IsRequired();
        builder.Property(c => c.Versao).HasMaxLength(80);
        builder.Property(c => c.Cor).HasMaxLength(40).IsRequired();
        builder.Property(c => c.Categoria).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Combustivel).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Cambio).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.ValorDiaria).HasPrecision(10, 2);
        builder.Property(c => c.Observacoes).HasMaxLength(500);

        builder.HasIndex(c => c.PlacaChave).IsUnique();
        builder.HasIndex(c => c.Renavam).IsUnique();
        builder.HasIndex(c => c.Chassi).IsUnique();
        builder.HasIndex(c => new { c.LojaAtualId, c.Status });
        builder.HasOne<Loja>().WithMany().HasForeignKey(c => c.LojaAtualId).OnDelete(DeleteBehavior.Restrict);
    }
}
