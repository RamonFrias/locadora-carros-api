using LocadoraCarros.Domain.Common;

namespace LocadoraCarros.Domain.ValueObjects;

/// <summary>Carteira Nacional de Habilitação.</summary>
public sealed record Cnh
{
    private static readonly HashSet<string> Categorias = ["A", "B", "C", "D", "E", "AB", "AC", "AD", "AE"];

    public string Numero { get; private init; }
    public string Categoria { get; private init; }
    public DateOnly Validade { get; private init; }

    private Cnh(string numero, string categoria, DateOnly validade)
    {
        Numero = numero;
        Categoria = categoria;
        Validade = validade;
    }

    public static Cnh Criar(string? numero, string? categoria, DateOnly validade)
    {
        var digitos = Guard.SomenteDigitos(numero);
        Guard.Garantir(digitos.Length == 11, "Número da CNH inválido. Informe os 11 dígitos do registro.");

        var categoriaNormalizada = categoria?.Trim().ToUpperInvariant() ?? string.Empty;
        Guard.Garantir(Categorias.Contains(categoriaNormalizada), "Categoria da CNH inválida.");

        return new Cnh(digitos, categoriaNormalizada, validade);
    }

    /// <summary>Categorias B a E (ou combinadas com A) habilitam a conduzir automóveis.</summary>
    public bool HabilitaAutomovel => Categoria != "A";

    public bool ValidaEm(DateOnly data) => Validade >= data;
}
