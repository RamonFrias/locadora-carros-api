using LocadoraCarros.Domain.Common;

namespace LocadoraCarros.Domain.Locacoes;

public enum TipoMovimentoLoja
{
    Todos = 0,
    Retirada = 1,
    Devolucao = 2
}

public sealed record FiltroLocacoes(
    Guid? ClienteId = null,
    Guid? CarroId = null,
    Guid? LojaId = null,
    TipoMovimentoLoja Movimento = TipoMovimentoLoja.Todos,
    StatusLocacao? Status = null,
    DateTime? De = null,
    DateTime? Ate = null,
    string? Codigo = null);

public interface ILocacaoRepository
{
    Task<Locacao?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<ResultadoPaginado<Locacao>> ListarAsync(FiltroLocacoes filtro, int pagina, int tamanhoPagina, CancellationToken ct = default);

    /// <summary>Existe reserva ou locação em andamento do carro que se sobreponha ao período?</summary>
    Task<bool> ExisteConflitoAsync(Guid carroId, DateTime inicio, DateTime fim, Guid? ignorarLocacaoId = null,
        CancellationToken ct = default);

    Task<bool> ClientePossuiLocacoesAtivasAsync(Guid clienteId, CancellationToken ct = default);
    Task<bool> CarroPossuiLocacoesAtivasAsync(Guid carroId, CancellationToken ct = default);
    Task<bool> LojaPossuiLocacoesAtivasAsync(Guid lojaId, CancellationToken ct = default);
    void Adicionar(Locacao locacao);
}
