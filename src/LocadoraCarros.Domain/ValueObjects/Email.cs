using System.Text.RegularExpressions;
using LocadoraCarros.Domain.Common;

namespace LocadoraCarros.Domain.ValueObjects;

public sealed partial record Email
{
    public string Valor { get; }

    private Email(string valor) => Valor = valor;

    public static Email Criar(string? valor)
    {
        var email = valor?.Trim().ToLowerInvariant() ?? string.Empty;
        if (email.Length > 254 || !Formato().IsMatch(email))
            throw new DomainException("E-mail inválido.");
        return new Email(email);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Formato();

    public override string ToString() => Valor;
}
