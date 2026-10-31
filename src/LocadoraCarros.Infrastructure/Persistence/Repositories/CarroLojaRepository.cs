using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace LocadoraCarros.Infrastructure.Persistence.Repositories;

internal sealed class CarroRepository(AppDbContext db) : ICarroRepository
{
    private static readonly StatusCarro[] StatusReservaveis = [StatusCarro.Disponivel, StatusCarro.Alugado, StatusCarro.EmLimpeza];

    public Task<Carro?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Carros.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyDictionary<Guid, Carro>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();
        return await db.Carros.Where(c => lista.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
    }

    public Task<ResultadoPaginado<Carro>> ListarAsync(FiltroCarros filtro, int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = db.Carros.AsNoTracking();
        if (filtro.LojaId is not null) query = query.Where(c => c.LojaAtualId == filtro.LojaId);
        if (filtro.Categoria is not null) query = query.Where(c => c.Categoria == filtro.Categoria);
        if (filtro.Status is not null) query = query.Where(c => c.Status == filtro.Status);
        else if (!filtro.IncluirInativos) query = query.Where(c => c.Status != StatusCarro.Inativo);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var placa = TentarPlaca(filtro.Busca);
            var padrao = Paginacao.Like(filtro.Busca);
            query = query.Where(c => c.PlacaChave == placa || EF.Functions.Like(c.Marca, padrao) || EF.Functions.Like(c.Modelo, padrao));
        }

        return query.OrderBy(c => c.Marca).ThenBy(c => c.Modelo).ThenBy(c => c.PlacaChave).PaginarAsync(pagina, tamanhoPagina, ct);
    }

    public async Task<IReadOnlyList<Carro>> ListarDisponiveisAsync(Guid lojaId, DateTime inicio, DateTime fim,
        CategoriaCarro? categoria, CancellationToken ct = default)
    {
        var carros = await db.Carros.AsNoTracking()
            .Where(c => c.LojaAtualId == lojaId
                        && StatusReservaveis.Contains(c.Status)
                        && (categoria == null || c.Categoria == categoria)
                        && !db.Locacoes.Any(l => l.CarroId == c.Id
                                                 && (l.Status == StatusLocacao.Reservada || l.Status == StatusLocacao.EmAndamento)
                                                 && l.DataRetiradaPrevista < fim
                                                 && inicio < l.DataDevolucaoPrevista))
            .ToListAsync(ct);

        // Ordenação em memória: o SQLite não ordena colunas decimal.
        return carros.OrderBy(c => c.ValorDiaria).ThenBy(c => c.Modelo).ToList();
    }

    public async Task<IReadOnlyDictionary<StatusCarro, int>> ContarPorStatusAsync(Guid lojaId, CancellationToken ct = default) =>
        await db.Carros.Where(c => c.LojaAtualId == lojaId)
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Quantidade = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Quantidade, ct);

    public Task<bool> ExistePlacaAsync(string placaChave, Guid? ignorarId = null, CancellationToken ct = default) =>
        db.Carros.AnyAsync(c => c.PlacaChave == placaChave && c.Id != ignorarId, ct);

    public Task<bool> ExisteRenavamAsync(string renavam, Guid? ignorarId = null, CancellationToken ct = default) =>
        db.Carros.AnyAsync(c => c.Renavam == renavam && c.Id != ignorarId, ct);

    public Task<bool> ExisteChassiAsync(string chassi, Guid? ignorarId = null, CancellationToken ct = default) =>
        db.Carros.AnyAsync(c => c.Chassi == chassi && c.Id != ignorarId, ct);

    public void Adicionar(Carro carro) => db.Carros.Add(carro);

    private static string? TentarPlaca(string texto)
    {
        try { return Placa.Criar(texto).EquivalenteMercosul; }
        catch (DomainException) { return null; }
    }
}

internal sealed class LojaRepository(AppDbContext db) : ILojaRepository
{
    public Task<Loja?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Lojas.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IReadOnlyDictionary<Guid, Loja>> ObterPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();
        return await db.Lojas.Where(l => lista.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);
    }

    public async Task<IReadOnlyList<Loja>> ListarAsync(string? cidade, string? uf, bool incluirInativas, CancellationToken ct = default)
    {
        var query = db.Lojas.AsNoTracking();
        if (!incluirInativas) query = query.Where(l => l.Ativa);
        if (!string.IsNullOrWhiteSpace(uf))
        {
            var ufNormalizada = uf.Trim().ToUpperInvariant();
            query = query.Where(l => l.Endereco.Uf == ufNormalizada);
        }
        if (!string.IsNullOrWhiteSpace(cidade))
        {
            var padrao = Paginacao.Like(cidade);
            query = query.Where(l => EF.Functions.Like(l.Endereco.Cidade, padrao));
        }
        return await query.OrderBy(l => l.Endereco.Uf).ThenBy(l => l.Endereco.Cidade).ThenBy(l => l.Nome).ToListAsync(ct);
    }

    public Task<bool> ExisteCodigoAsync(string codigo, Guid? ignorarId = null, CancellationToken ct = default) =>
        db.Lojas.AnyAsync(l => l.Codigo == codigo && l.Id != ignorarId, ct);

    public Task<bool> ExisteCnpjAsync(Cnpj cnpj, Guid? ignorarId = null, CancellationToken ct = default) =>
        db.Lojas.AnyAsync(l => l.Cnpj == cnpj && l.Id != ignorarId, ct);

    public void Adicionar(Loja loja) => db.Lojas.Add(loja);
}
