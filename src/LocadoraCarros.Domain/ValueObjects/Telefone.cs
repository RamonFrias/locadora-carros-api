using LocadoraCarros.Domain.Common;

namespace LocadoraCarros.Domain.ValueObjects;

/// <summary>Telefone brasileiro com DDD: 10 dígitos (fixo) ou 11 (celular).</summary>
public sealed record Telefone
{
    public string Valor { get; }

    private Telefone(string valor) => Valor = valor;

    public static Telefone Criar(string? valor)
    {
        var digitos = Guard.SomenteDigitos(valor);
        if (digitos.Length is 12 or 13 && digitos.StartsWith("55"))
            digitos = digitos[2..];

        if (digitos.Length is not (10 or 11) || digitos[0] == '0')
            throw new DomainException("Telefone inválido. Informe DDD + número.");
        return new Telefone(digitos);
    }

    public string Formatado => Valor.Length == 11
        ? $"({Valor[..2]}) {Valor[2..7]}-{Valor[7..]}"
        : $"({Valor[..2]}) {Valor[2..6]}-{Valor[6..]}";

    public override string ToString() => Formatado;
}
