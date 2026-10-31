using System.Text.RegularExpressions;
using LocadoraCarros.Domain.Common;

namespace LocadoraCarros.Domain.ValueObjects;

public enum PadraoPlaca
{
    Antiga = 1,   // ABC-1234
    Mercosul = 2  // ABC1D23
}

public sealed partial record Placa
{
    public string Valor { get; }
    public PadraoPlaca Padrao { get; }

    private Placa(string valor, PadraoPlaca padrao)
    {
        Valor = valor;
        Padrao = padrao;
    }

    public static Placa Criar(string? valor)
    {
        var normalizada = new string((valor ?? string.Empty).ToUpperInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());

        if (FormatoMercosul().IsMatch(normalizada)) return new Placa(normalizada, PadraoPlaca.Mercosul);
        if (FormatoAntigo().IsMatch(normalizada)) return new Placa(normalizada, PadraoPlaca.Antiga);

        throw new DomainException("Placa inválida. Use o padrão antigo (ABC-1234) ou Mercosul (ABC1D23).");
    }

    public string Formatada => Padrao == PadraoPlaca.Antiga ? $"{Valor[..3]}-{Valor[3..]}" : Valor;

    /// <summary>
    /// Placa no padrão Mercosul. Na conversão oficial o 5º caractere (dígito) vira letra: 0=A, 1=B ... 9=J.
    /// Serve como chave única, já que ABC-1234 e ABC1C34 identificam o mesmo veículo.
    /// </summary>
    public string EquivalenteMercosul => Padrao == PadraoPlaca.Mercosul
        ? Valor
        : string.Concat(Valor.AsSpan(0, 4), ((char)('A' + (Valor[4] - '0'))).ToString(), Valor.AsSpan(5));

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z][0-9]{2}$")]
    private static partial Regex FormatoMercosul();

    [GeneratedRegex("^[A-Z]{3}[0-9]{4}$")]
    private static partial Regex FormatoAntigo();

    public override string ToString() => Formatada;
}
