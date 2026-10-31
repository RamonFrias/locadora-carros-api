using LocadoraCarros.Application.Abstractions;

namespace LocadoraCarros.Infrastructure;

/// <summary>
/// Horário de Brasília. Datas de retirada/devolução são tratadas como horário local das lojas
/// (sem fuso), então a API espera datas sem offset, ex.: 2026-11-10T09:00:00.
/// </summary>
internal sealed class RelogioBrasilia : IRelogio
{
    private static readonly TimeZoneInfo Fuso = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public DateTime Agora => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Fuso), DateTimeKind.Unspecified);
}
