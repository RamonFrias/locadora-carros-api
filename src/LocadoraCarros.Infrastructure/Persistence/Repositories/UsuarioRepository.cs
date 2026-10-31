using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace LocadoraCarros.Infrastructure.Persistence.Repositories;

internal sealed class UsuarioRepository(AppDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Cliente?> ObterClienteAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.OfType<Cliente>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Funcionario?> ObterFuncionarioAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.OfType<Funcionario>().FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<Usuario?> ObterPorCpfAsync(Cpf cpf, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Cpf == cpf, ct);

    public async Task<IReadOnlyDictionary<Guid, Usuario>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();
        return await db.Usuarios.Where(u => lista.Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);
    }

    public Task<bool> ExisteCpfAsync(Cpf cpf, Guid? ignorarId = null, CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(u => u.Cpf == cpf && u.Id != ignorarId, ct);

    public Task<bool> ExisteEmailAsync(Email email, Guid? ignorarId = null, CancellationToken ct = default) =>
        db.Usuarios.AnyAsync(u => u.Email == email && u.Id != ignorarId, ct);

    public Task<bool> ExisteMatriculaAsync(string matricula, Guid? ignorarId = null, CancellationToken ct = default) =>
        db.Usuarios.OfType<Funcionario>().AnyAsync(f => f.Matricula == matricula && f.Id != ignorarId, ct);

    public Task<bool> LojaPossuiFuncionariosAtivosAsync(Guid lojaId, CancellationToken ct = default) =>
        db.Usuarios.OfType<Funcionario>().AnyAsync(f => f.LojaId == lojaId && f.Ativo, ct);

    public Task<ResultadoPaginado<Cliente>> ListarClientesAsync(string? busca, bool incluirInativos, int pagina,
        int tamanhoPagina, CancellationToken ct = default)
    {
        var query = db.Usuarios.OfType<Cliente>().AsNoTracking();
        if (!incluirInativos) query = query.Where(c => c.Ativo);
        if (!string.IsNullOrWhiteSpace(busca) && !TentarFiltroExato(ref query, busca))
        {
            var padrao = Paginacao.Like(busca);
            query = query.Where(c => EF.Functions.Like(c.Nome, padrao));
        }
        return query.OrderBy(c => c.Nome).PaginarAsync(pagina, tamanhoPagina, ct);
    }

    public Task<ResultadoPaginado<Funcionario>> ListarFuncionariosAsync(Guid? lojaId, string? busca, bool incluirInativos,
        int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = db.Usuarios.OfType<Funcionario>().AsNoTracking();
        if (lojaId is not null) query = query.Where(f => f.LojaId == lojaId);
        if (!incluirInativos) query = query.Where(f => f.Ativo);
        if (!string.IsNullOrWhiteSpace(busca) && !TentarFiltroExato(ref query, busca))
        {
            var padrao = Paginacao.Like(busca);
            var matricula = busca.Trim().ToUpperInvariant();
            query = query.Where(f => EF.Functions.Like(f.Nome, padrao) || f.Matricula == matricula);
        }
        return query.OrderBy(f => f.Nome).PaginarAsync(pagina, tamanhoPagina, ct);
    }

    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);

    /// <summary>
    /// CPF e e-mail são value objects convertidos, então a busca neles é por igualdade exata.
    /// Retorna false quando o termo não é CPF nem e-mail (o chamador aplica a busca parcial por nome).
    /// </summary>
    private static bool TentarFiltroExato<T>(ref IQueryable<T> query, string busca) where T : Usuario
    {
        busca = busca.Trim();
        var digitos = new string(busca.Where(char.IsDigit).ToArray());
        if (Cpf.EhValido(digitos))
        {
            var cpf = Cpf.Criar(digitos);
            query = query.Where(u => u.Cpf == cpf);
            return true;
        }
        if (!busca.Contains('@')) return false;

        try
        {
            var email = Email.Criar(busca);
            query = query.Where(u => u.Email == email);
        }
        catch (DomainException)
        {
            query = query.Where(_ => false);
        }
        return true;
    }
}
