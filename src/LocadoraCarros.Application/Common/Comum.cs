using System.ComponentModel.DataAnnotations;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Application.Common;

public record PaginacaoRequest
{
    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 100)]
    public int TamanhoPagina { get; init; } = 20;
}

public sealed record EnderecoDto(
    [Required, StringLength(150)] string Logradouro,
    [Required, StringLength(20)] string Numero,
    [StringLength(100)] string? Complemento,
    [Required, StringLength(100)] string Bairro,
    [Required, StringLength(100)] string Cidade,
    [Required, StringLength(2, MinimumLength = 2)] string Uf,
    [Required] string Cep)
{
    public Endereco ParaDominio() => Endereco.Criar(Logradouro, Numero, Complemento, Bairro, Cidade, Uf, Cep);

    public static EnderecoDto De(Endereco e) =>
        new(e.Logradouro, e.Numero, e.Complemento, e.Bairro, e.Cidade, e.Uf, e.CepFormatado);
}

public sealed record CnhDto(
    [Required] string Numero,
    [Required] string Categoria,
    DateOnly Validade)
{
    public Cnh ParaDominio() => Cnh.Criar(Numero, Categoria, Validade);

    public static CnhDto De(Cnh c) => new(c.Numero, c.Categoria, c.Validade);
}

public static class PoliticaSenha
{
    public const int TamanhoMinimo = 8;

    public static void Validar(string? senha)
    {
        if (string.IsNullOrEmpty(senha) || senha.Length < TamanhoMinimo || senha.Length > 100
            || !senha.Any(char.IsLetter) || !senha.Any(char.IsDigit))
            throw new ValidacaoException($"A senha deve ter de {TamanhoMinimo} a 100 caracteres, com letras e números.");
    }
}
