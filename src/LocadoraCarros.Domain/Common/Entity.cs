namespace LocadoraCarros.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    // Preenchidos pela camada de persistência no momento do SaveChanges.
    public DateTime CriadoEm { get; private set; }
    public DateTime? AtualizadoEm { get; private set; }

    public override bool Equals(object? obj) =>
        obj is Entity outra && outra.GetType() == GetType() && outra.Id == Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

/// <summary>Marca a raiz de um agregado: só ela é carregada e persistida pelos repositórios.</summary>
public abstract class AggregateRoot : Entity;
