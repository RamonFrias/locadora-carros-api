using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Domain.Usuarios;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<Cliente?> ObterClienteAsync(Guid id, CancellationToken ct = default);
    Task<Funcionario?> ObterFuncionarioAsync(Guid id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct = default);
    Task<Usuario?> ObterPorCpfAsync(Cpf cpf, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, Usuario>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task<bool> ExisteCpfAsync(Cpf cpf, Guid? ignorarId = null, CancellationToken ct = default);
    Task<bool> ExisteEmailAsync(Email email, Guid? ignorarId = null, CancellationToken ct = default);
    Task<bool> ExisteMatriculaAsync(string matricula, Guid? ignorarId = null, CancellationToken ct = default);
    Task<bool> LojaPossuiFuncionariosAtivosAsync(Guid lojaId, CancellationToken ct = default);

    Task<ResultadoPaginado<Cliente>> ListarClientesAsync(string? busca, bool incluirInativos, int pagina,
        int tamanhoPagina, CancellationToken ct = default);

    Task<ResultadoPaginado<Funcionario>> ListarFuncionariosAsync(Guid? lojaId, string? busca, bool incluirInativos,
        int pagina, int tamanhoPagina, CancellationToken ct = default);

    void Adicionar(Usuario usuario);
}
