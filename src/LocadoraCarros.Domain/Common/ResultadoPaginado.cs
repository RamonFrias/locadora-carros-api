namespace LocadoraCarros.Domain.Common;

public sealed record ResultadoPaginado<T>(IReadOnlyList<T> Itens, int Pagina, int TamanhoPagina, int TotalItens)
{
    public int TotalPaginas => TamanhoPagina == 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);

    public ResultadoPaginado<TDestino> Mapear<TDestino>(Func<T, TDestino> mapeador) =>
        new(Itens.Select(mapeador).ToList(), Pagina, TamanhoPagina, TotalItens);
}

public interface IUnitOfWork
{
    Task<int> SalvarAsync(CancellationToken ct = default);
}
