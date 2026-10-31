using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LocadoraCarros.Infrastructure.Persistence.Configurations;

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);

        // TPH: clientes, funcionários e gerentes na mesma tabela, diferenciados pelo perfil.
        builder.HasDiscriminator(u => u.Perfil)
            .HasValue<Cliente>(PerfilUsuario.Cliente)
            .HasValue<Funcionario>(PerfilUsuario.Funcionario)
            .HasValue<Gerente>(PerfilUsuario.Gerente);
        builder.Property(u => u.Perfil).HasConversion<string>().HasMaxLength(20);

        builder.Property(u => u.Nome).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Cpf).HasConversion(v => v.Valor, v => Cpf.Criar(v)).HasMaxLength(11).IsRequired();
        builder.Property(u => u.Email).HasConversion(v => v.Valor, v => Email.Criar(v)).HasMaxLength(254).IsRequired();
        builder.Property(u => u.Telefone).HasConversion(v => v.Valor, v => Telefone.Criar(v)).HasMaxLength(11).IsRequired();
        builder.Property(u => u.SenhaHash).HasMaxLength(500).IsRequired();

        builder.HasIndex(u => u.Cpf).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}

internal sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ComplexProperty(c => c.Endereco, e => e.MapearEndereco());
        builder.ComplexProperty(c => c.Cnh, cnh =>
        {
            cnh.Property(x => x.Numero).HasColumnName("CnhNumero").HasMaxLength(11);
            cnh.Property(x => x.Categoria).HasColumnName("CnhCategoria").HasMaxLength(2);
            cnh.Property(x => x.Validade).HasColumnName("CnhValidade");
        });
    }
}

internal sealed class FuncionarioConfiguration : IEntityTypeConfiguration<Funcionario>
{
    public void Configure(EntityTypeBuilder<Funcionario> builder)
    {
        builder.Property(f => f.Matricula).HasMaxLength(20);
        builder.Property(f => f.Cargo).HasMaxLength(80);
        builder.HasIndex(f => f.Matricula).IsUnique();
        builder.HasOne<Loja>().WithMany().HasForeignKey(f => f.LojaId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal static class EnderecoMapping
{
    public static void MapearEndereco(this ComplexPropertyBuilder<Endereco> e)
    {
        e.Property(x => x.Logradouro).HasColumnName("Logradouro").HasMaxLength(150);
        e.Property(x => x.Numero).HasColumnName("Numero").HasMaxLength(20);
        e.Property(x => x.Complemento).HasColumnName("Complemento").HasMaxLength(100);
        e.Property(x => x.Bairro).HasColumnName("Bairro").HasMaxLength(100);
        e.Property(x => x.Cidade).HasColumnName("Cidade").HasMaxLength(100);
        e.Property(x => x.Uf).HasColumnName("Uf").HasMaxLength(2);
        e.Property(x => x.Cep).HasColumnName("Cep").HasMaxLength(8);
    }
}
