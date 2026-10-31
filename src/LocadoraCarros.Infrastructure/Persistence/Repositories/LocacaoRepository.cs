using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Locacoes;
using Microsoft.EntityFrameworkCore;

namespace LocadoraCarros.Infrastructure.Persistence.Repositories;

internal sealed class LocacaoRepository(AppDbContext db) : ILocacaoRepository
{
    private IQueryable<Locacao> Ativas => db.Locacoes.Where(l =>
        l.Status == StatusLocacao.Reservada || l.Status == StatusLocacao.EmAndamento);

    public Task<Locacao?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Locacoes.FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<ResultadoPaginado<Locacao>> ListarAsync(FiltroLocacoes filtro, int pagina, int tamanhoPagina,
        CancellationToken ct = default)
    {
        var query = db.Locacoes.AsNoTracking();
        if (filtro.ClienteId is not null) query = query.Where(l => l.ClienteId == filtro.ClienteId);
        if (filtro.CarroId is not null) query = query.Where(l => l.CarroId == filtro.CarroId);
        if (filtro.Status is not null) query = query.Where(l => l.Status == filtro.Status);
        if (!string.IsNullOrWhiteSpace(filtro.Codigo))
        {
            var codigo = filtro.Codigo.Trim().ToUpperInvariant();
            query = query.Where(l => l.Codigo == codigo);
        }

        if (filtro.LojaId is not null)
        {
            query = filtro.Movimento switch
            {
                TipoMovimentoLoja.Retirada => query.Where(l => l.LojaRetiradaId == filtro.LojaId),
                TipoMovimentoLoja.Devolucao => query.Where(l => l.LojaDevolucaoId == filtro.LojaId),
                _ => query.Where(l => l.LojaRetiradaId == filtro.LojaId || l.LojaDevolucaoId == filtro.LojaId)
            };
        }

        // O período filtra pela data do movimento consultado.
        if (filtro.Movimento == TipoMovimentoLoja.Devolucao)
        {
            if (filtro.De is not null) query = query.Where(l => l.DataDevolucaoPrevista >= filtro.De);
            if (filtro.Ate is not null) query = query.Where(l => l.DataDevolucaoPrevista < filtro.Ate);
            return query.OrderBy(l => l.DataDevolucaoPrevista).PaginarAsync(pagina, tamanhoPagina, ct);
        }

        if (filtro.De is not null) query = query.Where(l => l.DataRetiradaPrevista >= filtro.De);
        if (filtro.Ate is not null) query = query.Where(l => l.DataRetiradaPrevista < filtro.Ate);
        return filtro.Movimento == TipoMovimentoLoja.Retirada
            ? query.OrderBy(l => l.DataRetiradaPrevista).PaginarAsync(pagina, tamanhoPagina, ct)
            : query.OrderByDescending(l => l.DataRetiradaPrevista).PaginarAsync(pagina, tamanhoPagina, ct);
    }

    public Task<bool> ExisteConflitoAsync(Guid carroId, DateTime inicio, DateTime fim, Guid? ignorarLocacaoId = null,
        CancellationToken ct = default) =>
        Ativas.AnyAsync(l => l.CarroId == carroId
                             && l.Id != ignorarLocacaoId
                             && l.DataRetiradaPrevista < fim
                             && inicio < l.DataDevolucaoPrevista, ct);

    public Task<bool> ClientePossuiLocacoesAtivasAsync(Guid clienteId, CancellationToken ct = default) =>
        Ativas.AnyAsync(l => l.ClienteId == clienteId, ct);

    public Task<bool> CarroPossuiLocacoesAtivasAsync(Guid carroId, CancellationToken ct = default) =>
        Ativas.AnyAsync(l => l.CarroId == carroId, ct);

    public Task<bool> LojaPossuiLocacoesAtivasAsync(Guid lojaId, CancellationToken ct = default) =>
        Ativas.AnyAsync(l => l.LojaRetiradaId == lojaId || l.LojaDevolucaoId == lojaId, ct);

    public void Adicionar(Locacao locacao) => db.Locacoes.Add(locacao);
}
