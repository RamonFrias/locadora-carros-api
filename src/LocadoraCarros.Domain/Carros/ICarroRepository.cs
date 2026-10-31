using LocadoraCarros.Domain.Common;

namespace LocadoraCarros.Domain.Carros;

public sealed record FiltroCarros(
    Guid? LojaId = null,
    CategoriaCarro? Categoria = null,
    StatusCarro? Status = null,
    string? Busca = null,
    bool IncluirInativos = false);

public interface ICarroRepository
{
    Task<Carro?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, Carro>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<ResultadoPaginado<Carro>> ListarAsync(FiltroCarros filtro, int pagina, int tamanhoPagina, CancellationToken ct = default);

    /// <summary>Carros da loja que aceitam reserva e não têm locação ativa sobrepondo o período.</summary>
    Task<IReadOnlyList<Carro>> ListarDisponiveisAsync(Guid lojaId, DateTime inicio, DateTime fim,
        CategoriaCarro? categoria, CancellationToken ct = default);

    Task<IReadOnlyDictionary<StatusCarro, int>> ContarPorStatusAsync(Guid lojaId, CancellationToken ct = default);
    Task<bool> ExistePlacaAsync(string placaChave, Guid? ignorarId = null, CancellationToken ct = default);
    Task<bool> ExisteRenavamAsync(string renavam, Guid? ignorarId = null, CancellationToken ct = default);
    Task<bool> ExisteChassiAsync(string chassi, Guid? ignorarId = null, CancellationToken ct = default);
    void Adicionar(Carro carro);
}
