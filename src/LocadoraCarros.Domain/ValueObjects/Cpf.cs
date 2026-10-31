using LocadoraCarros.Domain.Common;

namespace LocadoraCarros.Domain.ValueObjects;

public sealed record Cpf
{
    public string Valor { get; }

    private Cpf(string valor) => Valor = valor;

    public static Cpf Criar(string? valor)
    {
        var digitos = Guard.SomenteDigitos(valor);
        if (!EhValido(digitos))
            throw new DomainException("CPF inválido.");
        return new Cpf(digitos);
    }

    public static bool EhValido(string digitos)
    {
        if (digitos.Length != 11 || digitos.Distinct().Count() == 1) return false;

        int CalcularDigito(int quantidade)
        {
            var soma = 0;
            for (var i = 0; i < quantidade; i++)
                soma += (digitos[i] - '0') * (quantidade + 1 - i);
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        return CalcularDigito(9) == digitos[9] - '0' && CalcularDigito(10) == digitos[10] - '0';
    }

    public string Formatado => $"{Valor[..3]}.{Valor[3..6]}.{Valor[6..9]}-{Valor[9..]}";

    public override string ToString() => Formatado;
}
