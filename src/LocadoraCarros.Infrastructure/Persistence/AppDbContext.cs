using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LocadoraCarros.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, IRelogio relogio)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Loja> Lojas => Set<Loja>();
    public DbSet<Carro> Carros => Set<Carro>();
    public DbSet<Locacao> Locacoes => Set<Locacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // O provider oficial do MySQL devolve DateTime/TimeSpan para colunas date/time e não lê DateOnly/TimeOnly.
        if (Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
        {
            configurationBuilder.Properties<DateOnly>().HaveConversion<DateOnlyParaDateTime>().HaveColumnType("date");
            configurationBuilder.Properties<TimeOnly>().HaveConversion<TimeOnlyParaTimeSpan>().HaveColumnType("time");
        }
    }

    private sealed class DateOnlyParaDateTime() : ValueConverter<DateOnly, DateTime>(
        d => d.ToDateTime(TimeOnly.MinValue), d => DateOnly.FromDateTime(d));

    private sealed class TimeOnlyParaTimeSpan() : ValueConverter<TimeOnly, TimeSpan>(
        t => t.ToTimeSpan(), t => TimeOnly.FromTimeSpan(t));

    public Task<int> SalvarAsync(CancellationToken ct = default) => SaveChangesAsync(ct);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var agora = relogio.Agora;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
                entry.Property(e => e.CriadoEm).CurrentValue = agora;
            else if (entry.State == EntityState.Modified)
                entry.Property(e => e.AtualizadoEm).CurrentValue = agora;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
