using System.ComponentModel.DataAnnotations;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.Usuarios;

namespace LocadoraCarros.Application.Locacoes;

public sealed record AdicionalRequest(TipoAdicional Tipo, [Range(1, 4)] int Quantidade = 1);

public class CotacaoRequest
{
    public Guid CarroId { get; init; }
    public Guid LojaRetiradaId { get; init; }
    public Guid LojaDevolucaoId { get; init; }
    public DateTime DataRetirada { get; init; }
    public DateTime DataDevolucao { get; init; }
    public TipoProtecao Protecao { get; init; } = TipoProtecao.Basica;
    public IReadOnlyList<AdicionalRequest> Adicionais { get; init; } = [];
}

public sealed class ReservaRequest : CotacaoRequest
{
    /// <summary>Obrigatório quando a reserva é feita pela equipe. Ignorado para clientes (usa o próprio cadastro).</summary>
    public Guid? ClienteId { get; init; }
    public FormaPagamento FormaPagamento { get; init; }
    [StringLength(1000)] public string? Observacoes { get; init; }
}

public sealed record CancelarLocacaoRequest(
    [StringLength(500)] string? Motivo,
    bool ConcederReembolso = false); // apenas equipe: reembolso fora do prazo de 24h

public sealed record RetiradaRequest(
    [Range(0, int.MaxValue)] int Quilometragem,
    [Range(0, 8)] int CombustivelOitavos);

public sealed record DevolucaoRequest(
    [Range(0, int.MaxValue)] int Quilometragem,
    [Range(0, 8)] int CombustivelOitavos,
    [Range(0, 1_000_000)] decimal ValorDanos = 0,
    Guid? LojaDevolucaoId = null, // se o cliente devolver em loja diferente da reservada
    [StringLength(1000)] string? Observacoes = null);

public sealed record FiltroLocacoesRequest : PaginacaoRequest
{
    public Guid? ClienteId { get; init; }
    public Guid? CarroId { get; init; }
    public Guid? LojaId { get; init; }
    /// <summary>Com LojaId: Retirada (saem da loja), Devolucao (chegam na loja) ou Todos.</summary>
    public TipoMovimentoLoja Movimento { get; init; } = TipoMovimentoLoja.Todos;
    public StatusLocacao? Status { get; init; }
    /// <summary>Filtra pela data do movimento (retirada ou devolução prevista).</summary>
    public DateTime? De { get; init; }
    public DateTime? Ate { get; init; }
    public string? Codigo { get; init; }
}

public sealed record OrcamentoResponse(
    int QuantidadeDiarias,
    decimal ValorDiaria,
    decimal ValorDiarias,
    decimal ValorProtecao,
    decimal ValorAdicionais,
    decimal TaxaDevolucaoOutraLoja,
    decimal ValorTotal,
    decimal ValorCaucao,
    bool CarroDisponivelNoPeriodo)
{
    public static OrcamentoResponse De(Orcamento o, bool disponivel) => new(o.QuantidadeDiarias, o.ValorDiaria,
        o.ValorDiarias, o.ValorProtecao, o.ValorAdicionais, o.TaxaDevolucaoOutraLoja, o.ValorTotal, o.ValorCaucao, disponivel);
}

public sealed record ClienteLocacaoResponse(Guid Id, string Nome, string Cpf, string Email, string Telefone, string Cnh);

public sealed record ReservadaPorResponse(Guid Id, string Nome, PerfilUsuario Perfil, bool ProprioCliente);

public sealed record CarroLocacaoResponse(Guid Id, string Placa, string Marca, string Modelo, string Cor,
    CategoriaCarro Categoria, TipoCambio Cambio);

public sealed record MovimentoLocacaoResponse(
    Guid LojaId,
    string LojaNome,
    EnderecoDto Endereco,
    string Telefone,
    DateTime DataPrevista,
    DateTime? DataReal,
    int? Quilometragem,
    int? CombustivelOitavos,
    Guid? RegistradaPorId);

public sealed record AdicionalResponse(TipoAdicional Tipo, int Quantidade, decimal ValorDiario);

public sealed record ValoresLocacaoResponse(
    int QuantidadeDiarias,
    decimal ValorDiaria,
    decimal ValorDiarias,
    decimal ValorProtecao,
    decimal ValorAdicionais,
    decimal TaxaDevolucaoOutraLoja,
    decimal ValorTotalPrevisto,
    int QuantidadeDiariasExtras,
    decimal ValorDiariasExtras,
    decimal TaxaCombustivel,
    decimal ValorDanos,
    decimal ValorTotal);

public sealed record PagamentoResponse(
    FormaPagamento Forma,
    StatusPagamento Status,
    decimal ValorPago,
    decimal ValorReembolsado,
    decimal SaldoDevedor,
    DateTime? DataUltimoPagamento,
    DateTime? DataUltimoReembolso);

public sealed record CaucaoResponse(decimal Valor, StatusCaucao Status, decimal ValorRetido);

public sealed record CancelamentoResponse(DateTime CanceladaEm, Guid? CanceladaPorId, string? Motivo);

public sealed record LocacaoResponse(
    Guid Id,
    string Codigo,
    StatusLocacao Status,
    ClienteLocacaoResponse Cliente,
    ReservadaPorResponse ReservadaPor,
    CarroLocacaoResponse Carro,
    MovimentoLocacaoResponse Retirada,
    MovimentoLocacaoResponse Devolucao,
    TipoProtecao Protecao,
    IReadOnlyList<AdicionalResponse> Adicionais,
    ValoresLocacaoResponse Valores,
    PagamentoResponse Pagamento,
    CaucaoResponse Caucao,
    int? KmPercorridos,
    bool CancelamentoComReembolsoGarantido,
    CancelamentoResponse? Cancelamento,
    string? Observacoes,
    DateTime CriadoEm,
    DateTime? AtualizadoEm);

public sealed record LocacaoResumoResponse(
    Guid Id,
    string Codigo,
    StatusLocacao Status,
    Guid ClienteId,
    string ClienteNome,
    Guid CarroId,
    string Placa,
    string Modelo,
    Guid LojaRetiradaId,
    string LojaRetiradaNome,
    DateTime DataRetiradaPrevista,
    Guid LojaDevolucaoId,
    string LojaDevolucaoNome,
    DateTime DataDevolucaoPrevista,
    decimal ValorTotal,
    StatusPagamento StatusPagamento,
    PerfilUsuario ReservadaPorPerfil);
