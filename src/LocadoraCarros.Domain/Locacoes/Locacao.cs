using System.Security.Cryptography;
using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.Usuarios;

namespace LocadoraCarros.Domain.Locacoes;

/// <summary>Dados escolhidos na reserva (criação ou alteração).</summary>
public sealed record DadosReserva(
    Cliente Cliente,
    Carro Carro,
    Loja LojaRetirada,
    Loja LojaDevolucao,
    DateTime DataRetirada,
    DateTime DataDevolucao,
    TipoProtecao Protecao,
    IReadOnlyCollection<AdicionalLocacao> Adicionais,
    FormaPagamento FormaPagamento,
    string? Observacoes);

public sealed class Locacao : AggregateRoot
{
    private const string CaracteresCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private readonly List<AdicionalLocacao> _adicionais = [];

    private Locacao() { }

    public string Codigo { get; private set; } = null!;
    public StatusLocacao Status { get; private set; }

    // Partes envolvidas
    public Guid ClienteId { get; private set; }
    public Guid CarroId { get; private set; }
    public Guid LojaRetiradaId { get; private set; }
    public Guid LojaDevolucaoId { get; private set; }
    public Guid ReservadaPorUsuarioId { get; private set; }
    public PerfilUsuario ReservadaPorPerfil { get; private set; }

    // Período e itens contratados
    public DateTime DataRetiradaPrevista { get; private set; }
    public DateTime DataDevolucaoPrevista { get; private set; }
    public TipoProtecao Protecao { get; private set; }
    public IReadOnlyCollection<AdicionalLocacao> Adicionais => _adicionais.AsReadOnly();

    // Valores previstos
    public int QuantidadeDiarias { get; private set; }
    public decimal ValorDiaria { get; private set; }
    public decimal ValorDiarias { get; private set; }
    public decimal ValorProtecao { get; private set; }
    public decimal ValorAdicionais { get; private set; }
    public decimal TaxaDevolucaoOutraLoja { get; private set; }
    public decimal ValorTotalPrevisto { get; private set; }

    // Encargos apurados na devolução
    public int QuantidadeDiariasExtras { get; private set; }
    public decimal ValorDiariasExtras { get; private set; }
    public decimal TaxaCombustivel { get; private set; }
    public decimal ValorDanos { get; private set; }

    // Pagamento
    public FormaPagamento FormaPagamento { get; private set; }
    public StatusPagamento StatusPagamento { get; private set; }
    public decimal ValorPago { get; private set; }
    public decimal ValorReembolsado { get; private set; }
    public DateTime? DataUltimoPagamento { get; private set; }
    public DateTime? DataUltimoReembolso { get; private set; }

    // Caução (pré-autorização no cartão, bloqueada na retirada)
    public decimal ValorCaucao { get; private set; }
    public StatusCaucao StatusCaucao { get; private set; }
    public decimal ValorCaucaoRetido { get; private set; }

    // Retirada
    public DateTime? DataRetiradaReal { get; private set; }
    public int? KmRetirada { get; private set; }
    public int? CombustivelRetirada { get; private set; } // em oitavos do tanque (0 a 8)
    public Guid? RetiradaRegistradaPorId { get; private set; }

    // Devolução
    public DateTime? DataDevolucaoReal { get; private set; }
    public int? KmDevolucao { get; private set; }
    public int? CombustivelDevolucao { get; private set; }
    public Guid? DevolucaoRegistradaPorId { get; private set; }

    // Cancelamento
    public DateTime? CanceladaEm { get; private set; }
    public Guid? CanceladaPorId { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    public string? Observacoes { get; private set; }

    public decimal ValorTotal => ValorTotalPrevisto + ValorDiariasExtras + TaxaCombustivel + ValorDanos;
    public decimal ValorLiquidoPago => ValorPago - ValorReembolsado;
    public decimal SaldoDevedor => Status == StatusLocacao.Cancelada
        ? 0m
        : Math.Max(0m, ValorTotal - ValorLiquidoPago - ValorCaucaoRetido);
    public int? KmPercorridos => KmDevolucao - KmRetirada;
    public bool ReservadaPeloProprioCliente => ReservadaPorUsuarioId == ClienteId;

    public bool CancelamentoComReembolsoGarantido(DateTime agora) =>
        DataRetiradaPrevista - agora >= TabelaPrecos.AntecedenciaMinimaReembolso;

    public static Locacao Criar(DadosReserva dados, Usuario reservadaPor, DateTime agora)
    {
        Guard.Garantir(reservadaPor.Ativo, "Usuário inativo.");
        Guard.Garantir(reservadaPor.EhEquipe || reservadaPor.Id == dados.Cliente.Id,
            "Clientes só podem fazer reservas em nome próprio.");

        var locacao = new Locacao
        {
            Codigo = $"LOC-{agora:yyMMdd}-{RandomNumberGenerator.GetString(CaracteresCodigo, 5)}",
            Status = StatusLocacao.Reservada,
            ClienteId = dados.Cliente.Id,
            ReservadaPorUsuarioId = reservadaPor.Id,
            ReservadaPorPerfil = reservadaPor.Perfil,
            StatusPagamento = StatusPagamento.Pendente,
            StatusCaucao = StatusCaucao.Pendente
        };
        locacao.AplicarReserva(dados, agora);
        locacao.AjustarPagamentoAoTotal(agora);
        return locacao;
    }

    /// <summary>Altera datas, lojas, carro, proteção, adicionais ou forma de pagamento de uma reserva.</summary>
    public void Alterar(DadosReserva dados, Usuario solicitante, DateTime agora)
    {
        GarantirAcesso(solicitante);
        Guard.Garantir(Status == StatusLocacao.Reservada, "Só é possível alterar locações que ainda não foram retiradas.");
        Guard.Garantir(dados.Cliente.Id == ClienteId, "Não é possível trocar o cliente de uma locação.");

        AplicarReserva(dados, agora);
        AjustarPagamentoAoTotal(agora);
    }

    /// <summary>
    /// Cancela a reserva. Com 24h ou mais de antecedência da retirada o valor pago é reembolsado integralmente;
    /// depois disso o valor fica retido, salvo reembolso concedido pela equipe.
    /// </summary>
    public void Cancelar(Usuario solicitante, string? motivo, bool concederReembolso, DateTime agora)
    {
        GarantirAcesso(solicitante);
        Guard.Garantir(Status == StatusLocacao.Reservada, "Só é possível cancelar locações que ainda não foram retiradas.");
        Guard.Garantir(!concederReembolso || solicitante.EhEquipe,
            "Somente funcionários e gerentes podem conceder reembolso fora do prazo.");

        if ((CancelamentoComReembolsoGarantido(agora) || concederReembolso) && ValorLiquidoPago > 0)
            Reembolsar(ValorLiquidoPago, agora);

        Status = StatusLocacao.Cancelada;
        StatusCaucao = StatusCaucao.NaoAplicavel;
        CanceladaEm = agora;
        CanceladaPorId = solicitante.Id;
        MotivoCancelamento = Guard.TextoOpcional(motivo, "Motivo do cancelamento");
        AtualizarStatusPagamento();
    }

    public void RegistrarRetirada(Usuario atendente, Carro carro, int quilometragem, int combustivelOitavos, DateTime agora)
    {
        Guard.Garantir(atendente.EhEquipe, "A retirada deve ser registrada por um funcionário.");
        Guard.Garantir(Status == StatusLocacao.Reservada, "Esta locação não está aguardando retirada.");
        Guard.Garantir(carro.Id == CarroId, "Carro diferente do reservado.");
        Guard.Garantir(agora >= DataRetiradaPrevista - TabelaPrecos.AntecedenciaMaximaRetirada,
            $"A retirada só pode ser feita a partir de {TabelaPrecos.AntecedenciaMaximaRetirada.TotalHours:0}h antes do horário reservado.");
        Guard.Garantir(agora < DataDevolucaoPrevista, "O período da reserva já terminou.");
        ValidarCombustivel(combustivelOitavos);

        carro.RegistrarRetirada(quilometragem);

        // Saldo em aberto (pagamento em dinheiro ou diferença de alteração) é cobrado no balcão.
        if (SaldoDevedor > 0) RegistrarPagamento(SaldoDevedor, agora);

        Status = StatusLocacao.EmAndamento;
        StatusCaucao = StatusCaucao.Bloqueada;
        DataRetiradaReal = agora;
        KmRetirada = quilometragem;
        CombustivelRetirada = combustivelOitavos;
        RetiradaRegistradaPorId = atendente.Id;
        AtualizarStatusPagamento();
    }

    public void RegistrarDevolucao(Usuario atendente, Carro carro, Loja lojaRetirada, Loja lojaDevolucao,
        int quilometragem, int combustivelOitavos, decimal valorDanos, string? observacoes, DateTime agora)
    {
        Guard.Garantir(atendente.EhEquipe, "A devolução deve ser registrada por um funcionário.");
        Guard.Garantir(Status == StatusLocacao.EmAndamento, "Esta locação não está em andamento.");
        Guard.Garantir(carro.Id == CarroId && lojaRetirada.Id == LojaRetiradaId, "Dados da locação inconsistentes.");
        Guard.Garantir(valorDanos >= 0, "Valor de danos não pode ser negativo.");
        ValidarCombustivel(combustivelOitavos);

        if (lojaDevolucao.Id != LojaDevolucaoId)
            AlterarLojaDevolucaoNoBalcao(lojaRetirada, lojaDevolucao);

        carro.RegistrarDevolucao(quilometragem, lojaDevolucao.Id);

        var atraso = agora - DataDevolucaoPrevista - TabelaPrecos.ToleranciaAtraso;
        if (atraso > TimeSpan.Zero)
        {
            QuantidadeDiariasExtras = (int)Math.Ceiling(atraso.TotalHours / 24);
            var diariaCompleta = ValorDiaria + ValorProtecao / QuantidadeDiarias + _adicionais.Sum(a => a.ValorDiarioTotal);
            ValorDiariasExtras = decimal.Round(diariaCompleta * QuantidadeDiariasExtras, 2);
        }

        var oitavosFaltantes = Math.Max(0, (CombustivelRetirada ?? TabelaPrecos.OitavosTanqueCheio) - combustivelOitavos);
        TaxaCombustivel = oitavosFaltantes * TabelaPrecos.TaxaPorOitavoCombustivel;
        ValorDanos = decimal.Round(valorDanos, 2);

        // Encargos não pagos são descontados da caução; o que exceder fica como saldo devedor.
        var encargos = ValorTotal - ValorLiquidoPago;
        if (encargos < 0) Reembolsar(-encargos, agora);
        ValorCaucaoRetido = Math.Clamp(encargos, 0m, ValorCaucao);
        StatusCaucao = ValorCaucaoRetido == 0 ? StatusCaucao.Liberada
            : ValorCaucaoRetido == ValorCaucao ? StatusCaucao.Retida
            : StatusCaucao.ParcialmenteRetida;

        Status = StatusLocacao.Concluida;
        DataDevolucaoReal = agora;
        KmDevolucao = quilometragem;
        CombustivelDevolucao = combustivelOitavos;
        DevolucaoRegistradaPorId = atendente.Id;
        if (!string.IsNullOrWhiteSpace(observacoes))
            Observacoes = Guard.TextoOpcional(Observacoes is null ? observacoes : $"{Observacoes} | {observacoes}", "Observações", 2000);
        AtualizarStatusPagamento();
    }

    /// <summary>Quita no balcão o saldo devedor (ex.: encargos que excederam a caução).</summary>
    public void QuitarSaldo(Usuario atendente, DateTime agora)
    {
        Guard.Garantir(atendente.EhEquipe, "Somente a equipe pode registrar pagamentos no balcão.");
        Guard.Garantir(SaldoDevedor > 0, "Não há saldo devedor nesta locação.");
        RegistrarPagamento(SaldoDevedor, agora);
        AtualizarStatusPagamento();
    }

    private void AplicarReserva(DadosReserva dados, DateTime agora)
    {
        var (cliente, carro, retirada, devolucao) = (dados.Cliente, dados.Carro, dados.LojaRetirada, dados.LojaDevolucao);

        Guard.Garantir(Enum.IsDefined(dados.FormaPagamento), "Forma de pagamento inválida.");
        Guard.Garantir(Enum.IsDefined(dados.Protecao), "Proteção inválida.");
        Guard.Garantir(dados.DataRetirada > agora, "A data de retirada deve ser futura.");
        Guard.Garantir(dados.DataDevolucao > dados.DataRetirada, "A devolução deve ser posterior à retirada.");
        Guard.Garantir(TabelaPrecos.CalcularDiarias(dados.DataRetirada, dados.DataDevolucao) <= TabelaPrecos.MaximoDiarias,
            $"O período máximo de uma locação é de {TabelaPrecos.MaximoDiarias} diárias.");
        Guard.Garantir(retirada.Ativa && devolucao.Ativa, "Loja de retirada ou devolução inativa.");
        Guard.Garantir(retirada.Id == devolucao.Id || devolucao.AceitaDevolucaoDeOutrasLojas,
            $"A loja {devolucao.Nome} não aceita devoluções de outras lojas.");
        Guard.Garantir(retirada.EstaAbertaEm(dados.DataRetirada), $"A loja {retirada.Nome} está fechada no horário de retirada.");
        Guard.Garantir(devolucao.EstaAbertaEm(dados.DataDevolucao), $"A loja {devolucao.Nome} está fechada no horário de devolução.");
        Guard.Garantir(carro.LojaAtualId == retirada.Id, "O carro escolhido não está na loja de retirada.");
        Guard.Garantir(carro.AceitaReservas, $"O carro não está disponível para reservas (status: {carro.Status}).");
        Guard.Garantir(dados.Adicionais.Select(a => a.Tipo).Distinct().Count() == dados.Adicionais.Count,
            "Adicionais repetidos: informe a quantidade em um único item.");
        cliente.ValidarAptidaoParaLocacao(DateOnly.FromDateTime(dados.DataRetirada), DateOnly.FromDateTime(dados.DataDevolucao));

        var orcamento = Orcamento.Calcular(carro, retirada, devolucao, dados.DataRetirada, dados.DataDevolucao,
            dados.Protecao, dados.Adicionais);

        CarroId = carro.Id;
        LojaRetiradaId = retirada.Id;
        LojaDevolucaoId = devolucao.Id;
        DataRetiradaPrevista = dados.DataRetirada;
        DataDevolucaoPrevista = dados.DataDevolucao;
        Protecao = dados.Protecao;
        _adicionais.Clear();
        _adicionais.AddRange(dados.Adicionais);
        FormaPagamento = dados.FormaPagamento;
        Observacoes = Guard.TextoOpcional(dados.Observacoes, "Observações", 2000);

        QuantidadeDiarias = orcamento.QuantidadeDiarias;
        ValorDiaria = orcamento.ValorDiaria;
        ValorDiarias = orcamento.ValorDiarias;
        ValorProtecao = orcamento.ValorProtecao;
        ValorAdicionais = orcamento.ValorAdicionais;
        TaxaDevolucaoOutraLoja = orcamento.TaxaDevolucaoOutraLoja;
        ValorTotalPrevisto = orcamento.ValorTotal;
        ValorCaucao = orcamento.ValorCaucao;
    }

    private void AlterarLojaDevolucaoNoBalcao(Loja lojaRetirada, Loja lojaDevolucao)
    {
        Guard.Garantir(lojaDevolucao.Ativa, "Loja de devolução inativa.");
        Guard.Garantir(lojaDevolucao.Id == LojaRetiradaId || lojaDevolucao.AceitaDevolucaoDeOutrasLojas,
            $"A loja {lojaDevolucao.Nome} não aceita devoluções de outras lojas.");

        var novaTaxa = TabelaPrecos.TaxaDevolucao(lojaRetirada, lojaDevolucao);
        ValorTotalPrevisto += novaTaxa - TaxaDevolucaoOutraLoja;
        TaxaDevolucaoOutraLoja = novaTaxa;
        LojaDevolucaoId = lojaDevolucao.Id;
    }

    /// <summary>
    /// Pagamentos online (cartão/Pix) são capturados na hora; diferenças de valor geram cobrança ou estorno.
    /// Em dinheiro o pagamento acontece no balcão, na retirada.
    /// </summary>
    private void AjustarPagamentoAoTotal(DateTime agora)
    {
        var diferenca = ValorTotal - ValorLiquidoPago;
        if (diferenca < 0)
            Reembolsar(-diferenca, agora);
        else if (diferenca > 0 && FormaPagamento != FormaPagamento.Dinheiro)
            RegistrarPagamento(diferenca, agora);

        AtualizarStatusPagamento();
    }

    private void RegistrarPagamento(decimal valor, DateTime agora)
    {
        ValorPago += valor;
        DataUltimoPagamento = agora;
    }

    private void Reembolsar(decimal valor, DateTime agora)
    {
        ValorReembolsado += valor;
        DataUltimoReembolso = agora;
    }

    private void AtualizarStatusPagamento()
    {
        StatusPagamento = Status == StatusLocacao.Cancelada
            ? ValorLiquidoPago > 0 ? StatusPagamento.NaoReembolsado
                : ValorReembolsado > 0 ? StatusPagamento.Reembolsado
                : StatusPagamento.Cancelado
            : SaldoDevedor == 0 ? StatusPagamento.Pago
            : ValorLiquidoPago > 0 ? StatusPagamento.ParcialmentePago
            : StatusPagamento.Pendente;
    }

    private void GarantirAcesso(Usuario solicitante) =>
        Guard.Garantir(solicitante.EhEquipe || solicitante.Id == ClienteId, "Esta locação pertence a outro cliente.");

    private static void ValidarCombustivel(int oitavos) =>
        Guard.Garantir(oitavos is >= 0 and <= TabelaPrecos.OitavosTanqueCheio,
            "Nível de combustível deve ser informado em oitavos do tanque (0 a 8).");
}
