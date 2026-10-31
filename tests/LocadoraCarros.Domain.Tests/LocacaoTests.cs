using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Locacoes;
using static LocadoraCarros.Domain.Tests.Fabrica;

namespace LocadoraCarros.Domain.Tests;

public class LocacaoTests
{
    [Theory]
    [InlineData(25, 1)]   // 24h + 1h de tolerância
    [InlineData(26, 2)]
    [InlineData(72, 3)]
    [InlineData(2, 1)]    // mínimo de uma diária
    public void Calcula_diarias_com_tolerancia(int horas, int diarias) =>
        Assert.Equal(diarias, TabelaPrecos.CalcularDiarias(Agora, Agora.AddHours(horas)));

    [Fact]
    public void Reserva_calcula_valores_e_captura_pagamento_online()
    {
        var loja = Loja();
        var cliente = Cliente();
        var locacao = Reservar(cliente, Carro(loja), loja, loja, Agora.AddDays(2), Agora.AddDays(5), cliente,
            adicionais: AdicionalLocacao.Criar(TipoAdicional.Gps, 1));

        Assert.Equal(3, locacao.QuantidadeDiarias);
        Assert.Equal(300m, locacao.ValorDiarias);
        Assert.Equal(89.70m, locacao.ValorProtecao);
        Assert.Equal(45m, locacao.ValorAdicionais);
        Assert.Equal(434.70m, locacao.ValorTotalPrevisto);
        Assert.Equal(434.70m, locacao.ValorPago);
        Assert.Equal(StatusPagamento.Pago, locacao.StatusPagamento);
        Assert.Equal(700m, locacao.ValorCaucao);
        Assert.True(locacao.ReservadaPeloProprioCliente);
    }

    [Fact]
    public void Pagamento_em_dinheiro_fica_pendente_ate_a_retirada()
    {
        var loja = Loja();
        var cliente = Cliente();
        var locacao = Reservar(cliente, Carro(loja), loja, loja, Agora.AddDays(2), Agora.AddDays(3), cliente,
            FormaPagamento.Dinheiro);

        Assert.Equal(StatusPagamento.Pendente, locacao.StatusPagamento);
        Assert.Equal(locacao.ValorTotal, locacao.SaldoDevedor);
    }

    [Theory]
    [InlineData("São Paulo", "SP", 60)]
    [InlineData("Rio de Janeiro", "RJ", 350)]
    public void Devolucao_em_outra_loja_cobra_taxa(string cidade, string uf, decimal taxa)
    {
        var retirada = Loja();
        var devolucao = Loja("XX-02", cidade, uf, "11222333000262");
        var cliente = Cliente();
        var locacao = Reservar(cliente, Carro(retirada), retirada, devolucao, Agora.AddDays(1), Agora.AddDays(2), cliente);

        Assert.Equal(taxa, locacao.TaxaDevolucaoOutraLoja);
    }

    [Fact]
    public void Cancelamento_com_24h_de_antecedencia_reembolsa_integralmente()
    {
        var loja = Loja();
        var cliente = Cliente();
        var locacao = Reservar(cliente, Carro(loja), loja, loja, Agora.AddHours(24), Agora.AddDays(3), cliente);

        locacao.Cancelar(cliente, "Mudança de planos", false, Agora);

        Assert.Equal(StatusLocacao.Cancelada, locacao.Status);
        Assert.Equal(StatusPagamento.Reembolsado, locacao.StatusPagamento);
        Assert.Equal(locacao.ValorPago, locacao.ValorReembolsado);
    }

    [Fact]
    public void Cancelamento_com_menos_de_24h_retem_o_valor()
    {
        var loja = Loja();
        var cliente = Cliente();
        var locacao = Reservar(cliente, Carro(loja), loja, loja, Agora.AddHours(23), Agora.AddDays(3), cliente);

        locacao.Cancelar(cliente, null, false, Agora);

        Assert.Equal(StatusPagamento.NaoReembolsado, locacao.StatusPagamento);
        Assert.Equal(0m, locacao.ValorReembolsado);
    }

    [Fact]
    public void Somente_equipe_concede_reembolso_fora_do_prazo()
    {
        var loja = Loja();
        var cliente = Cliente();
        var locacao = Reservar(cliente, Carro(loja), loja, loja, Agora.AddHours(5), Agora.AddDays(3), cliente);

        Assert.Throws<DomainException>(() => locacao.Cancelar(cliente, null, true, Agora));

        locacao.Cancelar(Funcionario(loja.Id), "Pane no voo", true, Agora);
        Assert.Equal(StatusPagamento.Reembolsado, locacao.StatusPagamento);
    }

    [Fact]
    public void Cliente_nao_reserva_em_nome_de_outro()
    {
        var loja = Loja();
        var outro = Cliente("39053344705");
        Assert.Throws<DomainException>(() =>
            Reservar(outro, Carro(loja), loja, loja, Agora.AddDays(1), Agora.AddDays(2), Cliente()));
    }

    [Fact]
    public void Funcionario_reserva_em_nome_do_cliente()
    {
        var loja = Loja();
        var funcionario = Funcionario(loja.Id);
        var locacao = Reservar(Cliente(), Carro(loja), loja, loja, Agora.AddDays(1), Agora.AddDays(2), funcionario);

        Assert.Equal(funcionario.Id, locacao.ReservadaPorUsuarioId);
        Assert.False(locacao.ReservadaPeloProprioCliente);
    }

    [Fact]
    public void Condutor_com_menos_de_21_anos_nao_aluga()
    {
        var loja = Loja();
        var jovem = Cliente(nascimento: Hoje.AddYears(-20));
        Assert.Throws<DomainException>(() =>
            Reservar(jovem, Carro(loja), loja, loja, Agora.AddDays(1), Agora.AddDays(2), jovem));
    }

    [Fact]
    public void Alterar_para_periodo_menor_estorna_a_diferenca()
    {
        var loja = Loja();
        var cliente = Cliente();
        var carro = Carro(loja);
        var locacao = Reservar(cliente, carro, loja, loja, Agora.AddDays(2), Agora.AddDays(5), cliente);
        var pagoInicialmente = locacao.ValorPago;

        locacao.Alterar(new DadosReserva(cliente, carro, loja, loja, Agora.AddDays(2), Agora.AddDays(3),
            TipoProtecao.Basica, [], FormaPagamento.Pix, null), cliente, Agora);

        Assert.Equal(1, locacao.QuantidadeDiarias);
        Assert.Equal(pagoInicialmente - locacao.ValorTotal, locacao.ValorReembolsado);
        Assert.Equal(StatusPagamento.Pago, locacao.StatusPagamento);
    }

    [Fact]
    public void Fluxo_completo_cobra_atraso_e_combustivel_da_caucao()
    {
        var loja = Loja();
        var cliente = Cliente();
        var carro = Carro(loja);
        var atendente = Funcionario(loja.Id);
        var retirada = Agora.AddHours(1);
        var devolucaoPrevista = retirada.AddDays(2);
        var locacao = Reservar(cliente, carro, loja, loja, retirada, devolucaoPrevista, cliente);

        locacao.RegistrarRetirada(atendente, carro, 1_000, 8, Agora);
        Assert.Equal(StatusLocacao.EmAndamento, locacao.Status);
        Assert.Equal(StatusCarro.Alugado, carro.Status);
        Assert.Equal(StatusCaucao.Bloqueada, locacao.StatusCaucao);

        locacao.RegistrarDevolucao(atendente, carro, loja, loja, 1_450, 6, 0m, "Sem avarias", devolucaoPrevista.AddHours(5));

        Assert.Equal(1, locacao.QuantidadeDiariasExtras);
        Assert.Equal(129.90m, locacao.ValorDiariasExtras);  // diária + proteção
        Assert.Equal(90m, locacao.TaxaCombustivel);         // 2/8 do tanque
        Assert.Equal(219.90m, locacao.ValorCaucaoRetido);
        Assert.Equal(StatusCaucao.ParcialmenteRetida, locacao.StatusCaucao);
        Assert.Equal(0m, locacao.SaldoDevedor);
        Assert.Equal(StatusPagamento.Pago, locacao.StatusPagamento);
        Assert.Equal(450, locacao.KmPercorridos);
        Assert.Equal(StatusCarro.EmLimpeza, carro.Status);
        Assert.Equal(1_450, carro.Quilometragem);
    }

    [Fact]
    public void Encargos_acima_da_caucao_geram_saldo_devedor()
    {
        var loja = Loja();
        var cliente = Cliente();
        var carro = Carro(loja);
        var atendente = Funcionario(loja.Id);
        var locacao = Reservar(cliente, carro, loja, loja, Agora.AddHours(1), Agora.AddDays(1), cliente);

        locacao.RegistrarRetirada(atendente, carro, 1_000, 8, Agora);
        locacao.RegistrarDevolucao(atendente, carro, loja, loja, 1_100, 8, 1_000m, "Para-choque", Agora.AddDays(1));

        Assert.Equal(StatusCaucao.Retida, locacao.StatusCaucao);
        Assert.Equal(300m, locacao.SaldoDevedor);
        Assert.Equal(StatusPagamento.ParcialmentePago, locacao.StatusPagamento);

        locacao.QuitarSaldo(atendente, Agora.AddDays(1));
        Assert.Equal(StatusPagamento.Pago, locacao.StatusPagamento);
    }

    [Fact]
    public void Carro_alugado_nao_muda_status_manualmente()
    {
        var loja = Loja();
        var carro = Carro(loja);

        Assert.Throws<DomainException>(() => carro.AlterarStatus(StatusCarro.Alugado, null));
        carro.AlterarStatus(StatusCarro.EmManutencao, "Revisão 20 mil km");
        Assert.False(carro.AceitaReservas);
    }
}
