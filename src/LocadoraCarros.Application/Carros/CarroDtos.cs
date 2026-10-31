using System.ComponentModel.DataAnnotations;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Application.Carros;

public class AtualizarCarroRequest
{
    /// <summary>Padrão antigo (ABC-1234) ou Mercosul (ABC1D23).</summary>
    [Required] public string Placa { get; init; } = null!;
    [Required] public string Renavam { get; init; } = null!;
    [Required] public string Chassi { get; init; } = null!;
    [Required, StringLength(60)] public string Marca { get; init; } = null!;
    [Required, StringLength(60)] public string Modelo { get; init; } = null!;
    [StringLength(80)] public string? Versao { get; init; }
    public int AnoFabricacao { get; init; }
    public int AnoModelo { get; init; }
    [Required, StringLength(40)] public string Cor { get; init; } = null!;
    public CategoriaCarro Categoria { get; init; }
    public TipoCombustivel Combustivel { get; init; }
    public TipoCambio Cambio { get; init; }
    public int Portas { get; init; }
    public int Lugares { get; init; }
    public int PortaMalasLitros { get; init; }
    public bool ArCondicionado { get; init; }
    public bool DirecaoAssistida { get; init; }
    public bool Multimidia { get; init; }
    public decimal ValorDiaria { get; init; }
    [StringLength(500)] public string? Observacoes { get; init; }

    public DadosCarro ParaDominio() => new(
        Domain.ValueObjects.Placa.Criar(Placa), Renavam, Chassi, Marca, Modelo, Versao, AnoFabricacao, AnoModelo, Cor,
        Categoria, Combustivel, Cambio, Portas, Lugares, PortaMalasLitros, ArCondicionado, DirecaoAssistida,
        Multimidia, ValorDiaria, Observacoes);
}

public sealed class CriarCarroRequest : AtualizarCarroRequest
{
    public Guid LojaId { get; init; }
    [Range(0, int.MaxValue)] public int Quilometragem { get; init; }
}

public sealed record AlterarStatusCarroRequest(StatusCarro Status, [StringLength(500)] string? Observacao);

public sealed record TransferirCarroRequest(Guid LojaId);

public sealed record FiltroCarrosRequest : PaginacaoRequest
{
    public Guid? LojaId { get; init; }
    public CategoriaCarro? Categoria { get; init; }
    public StatusCarro? Status { get; init; }
    /// <summary>Placa, marca ou modelo.</summary>
    public string? Busca { get; init; }
    public bool IncluirInativos { get; init; }
}

public sealed class DisponibilidadeRequest
{
    [Required] public Guid? LojaId { get; init; }
    [Required] public DateTime? Inicio { get; init; }
    [Required] public DateTime? Fim { get; init; }
    public CategoriaCarro? Categoria { get; init; }
}

/// <summary>Visão do carro para o cliente escolher: sem placa, RENAVAM, chassi, quilometragem nem status interno.</summary>
public sealed record CarroDisponivelResponse(
    Guid Id,
    string Marca,
    string Modelo,
    string? Versao,
    int AnoModelo,
    string Cor,
    CategoriaCarro Categoria,
    TipoCombustivel Combustivel,
    TipoCambio Cambio,
    int Portas,
    int Lugares,
    int PortaMalasLitros,
    bool ArCondicionado,
    bool DirecaoAssistida,
    bool Multimidia,
    decimal ValorDiaria,
    decimal ValorCaucao,
    Guid LojaId)
{
    public static CarroDisponivelResponse De(Carro c) => new(
        c.Id, c.Marca, c.Modelo, c.Versao, c.AnoModelo, c.Cor, c.Categoria, c.Combustivel, c.Cambio, c.Portas,
        c.Lugares, c.PortaMalasLitros, c.ArCondicionado, c.DirecaoAssistida, c.Multimidia, c.ValorDiaria,
        TabelaPrecos.Caucao(c.Categoria), c.LojaAtualId);
}

public sealed record CarroResponse(
    Guid Id,
    string Placa,
    PadraoPlaca PadraoPlaca,
    string PlacaMercosul,
    string Renavam,
    string Chassi,
    string Marca,
    string Modelo,
    string? Versao,
    int AnoFabricacao,
    int AnoModelo,
    string Cor,
    CategoriaCarro Categoria,
    TipoCombustivel Combustivel,
    TipoCambio Cambio,
    int Portas,
    int Lugares,
    int PortaMalasLitros,
    bool ArCondicionado,
    bool DirecaoAssistida,
    bool Multimidia,
    int Quilometragem,
    decimal ValorDiaria,
    decimal ValorCaucao,
    Guid LojaAtualId,
    StatusCarro Status,
    bool AceitaReservas,
    string? Observacoes)
{
    public static CarroResponse De(Carro c) => new(
        c.Id, c.Placa.Formatada, c.Placa.Padrao, c.PlacaChave, c.Renavam, c.Chassi, c.Marca, c.Modelo, c.Versao,
        c.AnoFabricacao, c.AnoModelo, c.Cor, c.Categoria, c.Combustivel, c.Cambio, c.Portas, c.Lugares,
        c.PortaMalasLitros, c.ArCondicionado, c.DirecaoAssistida, c.Multimidia, c.Quilometragem, c.ValorDiaria,
        TabelaPrecos.Caucao(c.Categoria), c.LojaAtualId, c.Status, c.AceitaReservas, c.Observacoes);
}
