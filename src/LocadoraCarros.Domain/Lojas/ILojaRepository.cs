using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Domain.Lojas;

public interface ILojaRepository
{
    Task<Loja?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, Loja>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyList<Loja>> ListarAsync(string? cidade, string? uf, bool incluirInativas, CancellationToken ct = default);
    Task<bool> ExisteCodigoAsync(string codigo, Guid? ignorarId = null, CancellationToken ct = default);
    Task<bool> ExisteCnpjAsync(Cnpj cnpj, Guid? ignorarId = null, CancellationToken ct = default);
    void Adicionar(Loja loja);
}
