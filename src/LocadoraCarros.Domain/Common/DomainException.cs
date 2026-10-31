namespace LocadoraCarros.Domain.Common;

/// <summary>Violação de uma regra de negócio do domínio.</summary>
public class DomainException(string message) : Exception(message);

internal static class Guard
{
    public static string Texto(string? valor, string campo, int tamanhoMaximo = 150)
    {
        var texto = valor?.Trim();
        if (string.IsNullOrWhiteSpace(texto))
            throw new DomainException($"{campo} é obrigatório.");
        if (texto.Length > tamanhoMaximo)
            throw new DomainException($"{campo} deve ter no máximo {tamanhoMaximo} caracteres.");
        return texto;
    }

    public static string? TextoOpcional(string? valor, string campo, int tamanhoMaximo = 500)
    {
        var texto = valor?.Trim();
        if (string.IsNullOrEmpty(texto)) return null;
        if (texto.Length > tamanhoMaximo)
            throw new DomainException($"{campo} deve ter no máximo {tamanhoMaximo} caracteres.");
        return texto;
    }

    public static void Garantir(bool condicao, string mensagem)
    {
        if (!condicao) throw new DomainException(mensagem);
    }

    public static string SomenteDigitos(string? valor) =>
        new((valor ?? string.Empty).Where(char.IsDigit).ToArray());
}
