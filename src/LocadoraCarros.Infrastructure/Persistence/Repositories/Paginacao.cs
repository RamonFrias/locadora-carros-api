using LocadoraCarros.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace LocadoraCarros.Infrastructure.Persistence.Repositories;

internal static class Paginacao
{
    public static async Task<ResultadoPaginado<T>> PaginarAsync<T>(this IQueryable<T> query, int pagina, int tamanhoPagina,
        CancellationToken ct)
    {
        pagina = Math.Max(1, pagina);
        tamanhoPagina = Math.Clamp(tamanhoPagina, 1, 200);

        var total = await query.CountAsync(ct);
        var itens = await query.Skip((pagina - 1) * tamanhoPagina).Take(tamanhoPagina).ToListAsync(ct);
        return new ResultadoPaginado<T>(itens, pagina, tamanhoPagina, total);
    }

    public static string Like(string termo) => $"%{termo.Trim()}%";
}
