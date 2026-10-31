using LocadoraCarros.Domain.Common;

namespace LocadoraCarros.Domain.ValueObjects;

/// <summary>
/// CNPJ numérico ou alfanumérico (padrão da Receita Federal a partir de julho/2026):
/// 12 caracteres alfanuméricos + 2 dígitos verificadores.
/// </summary>
public sealed record Cnpj
{
    private static readonly int[] PesosPrimeiroDigito = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosSegundoDigito = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public string Valor { get; }

    private Cnpj(string valor) => Valor = valor;

    public static Cnpj Criar(string? valor)
    {
        var normalizado = new string((valor ?? string.Empty).ToUpperInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());
        if (!EhValido(normalizado))
            throw new DomainException("CNPJ inválido.");
        return new Cnpj(normalizado);
    }

    public static bool EhValido(string cnpj)
    {
        if (cnpj.Length != 14 || !char.IsAsciiDigit(cnpj[12]) || !char.IsAsciiDigit(cnpj[13])) return false;
        if (cnpj.Distinct().Count() == 1) return false;

        // Cada caractere vale (código ASCII - 48): dígitos mantêm o valor, letras A=17, B=18...
        int CalcularDigito(int[] pesos)
        {
            var soma = 0;
            for (var i = 0; i < pesos.Length; i++)
                soma += (cnpj[i] - '0') * pesos[i];
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        return CalcularDigito(PesosPrimeiroDigito) == cnpj[12] - '0'
            && CalcularDigito(PesosSegundoDigito) == cnpj[13] - '0';
    }

    public string Formatado => $"{Valor[..2]}.{Valor[2..5]}.{Valor[5..8]}/{Valor[8..12]}-{Valor[12..]}";

    public override string ToString() => Formatado;
}
