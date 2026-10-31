using System.ComponentModel.DataAnnotations;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Application.Locacoes;
using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Lojas;

namespace LocadoraCarros.Application.Lojas;

public sealed record LojaRequest(
    [Required, StringLength(20)] string Codigo,
    [Required, StringLength(150)] string Nome,
    [Required] string Cnpj,
    TipoLoja Tipo,
    [Required] EnderecoDto Endereco,
    [Required] string Telefone,
    [Required, EmailAddress] string Email,
    bool Funciona24Horas,
    TimeOnly? HorarioAbertura,
    TimeOnly? HorarioFechamento,
    bool AceitaDevolucaoDeOutrasLojas = true);

public sealed class FiltroLojasRequest
{
    public string? Cidade { get; init; }
    public string? Uf { get; init; }
    public bool IncluirInativas { get; init; }
}

public sealed record LojaResponse(
    Guid Id,
    string Codigo,
    string Nome,
    string Cnpj,
    TipoLoja Tipo,
    EnderecoDto Endereco,
    string Telefone,
    string Email,
    bool Funciona24Horas,
    TimeOnly HorarioAbertura,
    TimeOnly HorarioFechamento,
    bool AceitaDevolucaoDeOutrasLojas,
    bool Ativa)
{
    public static LojaResponse De(Loja l) => new(
        l.Id, l.Codigo, l.Nome, l.Cnpj.Formatado, l.Tipo, EnderecoDto.De(l.Endereco), l.Telefone.Formatado,
        l.Email.Valor, l.Funciona24Horas, l.HorarioAbertura, l.HorarioFechamento, l.AceitaDevolucaoDeOutrasLojas, l.Ativa);
}

/// <summary>Visão operacional da loja no dia.</summary>
public sealed record PainelLojaResponse(
    LojaResponse Loja,
    DateOnly Data,
    int TotalCarros,
    IReadOnlyDictionary<StatusCarro, int> CarrosPorStatus,
    IReadOnlyList<LocacaoResumoResponse> RetiradasDoDia,
    IReadOnlyList<LocacaoResumoResponse> DevolucoesDoDia,
    IReadOnlyList<LocacaoResumoResponse> DevolucoesAtrasadas,
    int LocacoesEmAndamentoComDevolucaoNestaLoja);
