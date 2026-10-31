using LocadoraCarros.Domain.Common;

namespace LocadoraCarros.Domain.ValueObjects;

public sealed record Endereco
{
    private static readonly HashSet<string> Ufs =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    ];

    public string Logradouro { get; private init; }
    public string Numero { get; private init; }
    public string? Complemento { get; private init; }
    public string Bairro { get; private init; }
    public string Cidade { get; private init; }
    public string Uf { get; private init; }
    public string Cep { get; private init; }

    private Endereco(string logradouro, string numero, string? complemento, string bairro, string cidade, string uf, string cep)
    {
        Logradouro = logradouro;
        Numero = numero;
        Complemento = complemento;
        Bairro = bairro;
        Cidade = cidade;
        Uf = uf;
        Cep = cep;
    }

    public static Endereco Criar(string? logradouro, string? numero, string? complemento, string? bairro,
        string? cidade, string? uf, string? cep)
    {
        var ufNormalizada = uf?.Trim().ToUpperInvariant() ?? string.Empty;
        Guard.Garantir(Ufs.Contains(ufNormalizada), "UF inválida.");

        var cepDigitos = Guard.SomenteDigitos(cep);
        Guard.Garantir(cepDigitos.Length == 8, "CEP inválido. Informe 8 dígitos.");

        return new Endereco(
            Guard.Texto(logradouro, "Logradouro"),
            Guard.Texto(numero, "Número", 20),
            Guard.TextoOpcional(complemento, "Complemento", 100),
            Guard.Texto(bairro, "Bairro", 100),
            Guard.Texto(cidade, "Cidade", 100),
            ufNormalizada,
            cepDigitos);
    }

    public bool MesmaCidade(Endereco outro) =>
        Uf == outro.Uf && string.Equals(Cidade, outro.Cidade, StringComparison.InvariantCultureIgnoreCase);

    public string CepFormatado => $"{Cep[..5]}-{Cep[5..]}";

    public override string ToString() =>
        $"{Logradouro}, {Numero}{(Complemento is null ? "" : $" - {Complemento}")}, {Bairro}, {Cidade}/{Uf}, {CepFormatado}";
}
