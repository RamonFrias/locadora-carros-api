using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.Usuarios;

namespace LocadoraCarros.Application.Locacoes;

public sealed class LocacaoService(
    ILocacaoRepository locacoes,
    ICarroRepository carros,
    ILojaRepository lojas,
    IUsuarioRepository usuarios,
    IUnitOfWork uow,
    IRelogio relogio,
    IUsuarioAtual usuarioAtual)
{
    public async Task<OrcamentoResponse> CotarAsync(CotacaoRequest request, CancellationToken ct)
    {
        if (request.DataDevolucao <= request.DataRetirada)
            throw new ValidacaoException("A devolução deve ser posterior à retirada.");

        var carro = await CarregarCarroAsync(request.CarroId, ct);
        var (retirada, devolucao) = await CarregarLojasAsync(request.LojaRetiradaId, request.LojaDevolucaoId, ct);
        var orcamento = Orcamento.Calcular(carro, retirada, devolucao, request.DataRetirada, request.DataDevolucao,
            request.Protecao, CriarAdicionais(request.Adicionais));

        var disponivel = carro.AceitaReservas && carro.LojaAtualId == retirada.Id
            && !await locacoes.ExisteConflitoAsync(carro.Id, request.DataRetirada, request.DataDevolucao, null, ct);
        return OrcamentoResponse.De(orcamento, disponivel);
    }

    public async Task<LocacaoResponse> CriarAsync(ReservaRequest request, CancellationToken ct)
    {
        var solicitante = await CarregarSolicitanteAsync(ct);
        var clienteId = solicitante switch
        {
            Cliente proprio when request.ClienteId is null || request.ClienteId == proprio.Id => proprio.Id,
            Cliente => throw new AcessoNegadoException("Clientes só podem fazer reservas em nome próprio."),
            _ => request.ClienteId ?? throw new ValidacaoException("Informe o cliente da reserva (ClienteId).")
        };
        var cliente = await usuarios.ObterClienteAsync(clienteId, ct) ?? throw new RecursoNaoEncontradoException("Cliente");

        var dados = await MontarDadosReservaAsync(cliente, request, ct);
        await GarantirSemConflitoAsync(dados, null, ct);

        var locacao = Locacao.Criar(dados, solicitante, relogio.Agora);
        locacoes.Adicionar(locacao);
        await uow.SalvarAsync(ct);
        return await MontarDetalheAsync(locacao, ct);
    }

    public async Task<ResultadoPaginado<LocacaoResumoResponse>> ListarAsync(FiltroLocacoesRequest filtro, CancellationToken ct)
    {
        // Clientes enxergam somente as próprias locações.
        var clienteId = usuarioAtual.EhEquipe ? filtro.ClienteId : usuarioAtual.Id;
        var resultado = await locacoes.ListarAsync(
            new FiltroLocacoes(clienteId, filtro.CarroId, filtro.LojaId, filtro.Movimento, filtro.Status, filtro.De,
                filtro.Ate, filtro.Codigo),
            filtro.Pagina, filtro.TamanhoPagina, ct);

        var resumos = await MontarResumosAsync(resultado.Itens, ct);
        return new ResultadoPaginado<LocacaoResumoResponse>(resumos, resultado.Pagina, resultado.TamanhoPagina, resultado.TotalItens);
    }

    public async Task<LocacaoResponse> ObterAsync(Guid id, CancellationToken ct) =>
        await MontarDetalheAsync(await CarregarAsync(id, ct), ct);

    public async Task<LocacaoResponse> AlterarAsync(Guid id, ReservaRequest request, CancellationToken ct)
    {
        var locacao = await CarregarAsync(id, ct);
        var solicitante = await CarregarSolicitanteAsync(ct);
        if (request.ClienteId is not null && request.ClienteId != locacao.ClienteId)
            throw new ValidacaoException("Não é possível trocar o cliente de uma locação. Cancele e crie uma nova reserva.");

        var cliente = await usuarios.ObterClienteAsync(locacao.ClienteId, ct) ?? throw new RecursoNaoEncontradoException("Cliente");
        var dados = await MontarDadosReservaAsync(cliente, request, ct);
        await GarantirSemConflitoAsync(dados, locacao.Id, ct);

        locacao.Alterar(dados, solicitante, relogio.Agora);
        await uow.SalvarAsync(ct);
        return await MontarDetalheAsync(locacao, ct);
    }

    public async Task<LocacaoResponse> CancelarAsync(Guid id, CancelarLocacaoRequest request, CancellationToken ct)
    {
        var locacao = await CarregarAsync(id, ct);
        var solicitante = await CarregarSolicitanteAsync(ct);
        if (request.ConcederReembolso && !solicitante.EhEquipe)
            throw new AcessoNegadoException("Somente funcionários e gerentes podem conceder reembolso fora do prazo.");

        locacao.Cancelar(solicitante, request.Motivo, request.ConcederReembolso, relogio.Agora);
        await uow.SalvarAsync(ct);
        return await MontarDetalheAsync(locacao, ct);
    }

    public async Task<LocacaoResponse> RegistrarRetiradaAsync(Guid id, RetiradaRequest request, CancellationToken ct)
    {
        var locacao = await CarregarAsync(id, ct);
        var atendente = await CarregarSolicitanteAsync(ct);
        var carro = await CarregarCarroAsync(locacao.CarroId, ct);

        locacao.RegistrarRetirada(atendente, carro, request.Quilometragem, request.CombustivelOitavos, relogio.Agora);
        await uow.SalvarAsync(ct);
        return await MontarDetalheAsync(locacao, ct);
    }

    public async Task<LocacaoResponse> RegistrarDevolucaoAsync(Guid id, DevolucaoRequest request, CancellationToken ct)
    {
        var locacao = await CarregarAsync(id, ct);
        var atendente = await CarregarSolicitanteAsync(ct);
        var carro = await CarregarCarroAsync(locacao.CarroId, ct);
        var (retirada, devolucao) = await CarregarLojasAsync(locacao.LojaRetiradaId,
            request.LojaDevolucaoId ?? locacao.LojaDevolucaoId, ct);

        locacao.RegistrarDevolucao(atendente, carro, retirada, devolucao, request.Quilometragem,
            request.CombustivelOitavos, request.ValorDanos, request.Observacoes, relogio.Agora);
        await uow.SalvarAsync(ct);
        return await MontarDetalheAsync(locacao, ct);
    }

    public async Task<LocacaoResponse> QuitarSaldoAsync(Guid id, CancellationToken ct)
    {
        var locacao = await CarregarAsync(id, ct);
        locacao.QuitarSaldo(await CarregarSolicitanteAsync(ct), relogio.Agora);
        await uow.SalvarAsync(ct);
        return await MontarDetalheAsync(locacao, ct);
    }

    public async Task<IReadOnlyList<LocacaoResumoResponse>> MontarResumosAsync(IReadOnlyList<Locacao> itens, CancellationToken ct)
    {
        if (itens.Count == 0) return [];

        var clientes = await usuarios.ObterPorIdsAsync(itens.Select(l => l.ClienteId).Distinct(), ct);
        var frota = await carros.ObterPorIdsAsync(itens.Select(l => l.CarroId).Distinct(), ct);
        var unidades = await lojas.ObterPorIdsAsync(itens.SelectMany(l => new[] { l.LojaRetiradaId, l.LojaDevolucaoId }).Distinct(), ct);

        return itens.Select(l => new LocacaoResumoResponse(
            l.Id, l.Codigo, l.Status,
            l.ClienteId, clientes.GetValueOrDefault(l.ClienteId)?.Nome ?? "-",
            l.CarroId, frota.GetValueOrDefault(l.CarroId)?.Placa.Formatada ?? "-",
            frota.TryGetValue(l.CarroId, out var c) ? $"{c.Marca} {c.Modelo}" : "-",
            l.LojaRetiradaId, unidades.GetValueOrDefault(l.LojaRetiradaId)?.Nome ?? "-", l.DataRetiradaPrevista,
            l.LojaDevolucaoId, unidades.GetValueOrDefault(l.LojaDevolucaoId)?.Nome ?? "-", l.DataDevolucaoPrevista,
            l.ValorTotal, l.StatusPagamento, l.ReservadaPorPerfil)).ToList();
    }

    private async Task<LocacaoResponse> MontarDetalheAsync(Locacao l, CancellationToken ct)
    {
        var pessoas = await usuarios.ObterPorIdsAsync([l.ClienteId, l.ReservadaPorUsuarioId], ct);
        var cliente = (Cliente)pessoas[l.ClienteId];
        var reservadaPor = pessoas[l.ReservadaPorUsuarioId];
        var carro = await CarregarCarroAsync(l.CarroId, ct);
        var unidades = await lojas.ObterPorIdsAsync([l.LojaRetiradaId, l.LojaDevolucaoId], ct);

        static MovimentoLocacaoResponse Movimento(Loja loja, DateTime prevista, DateTime? real, int? km, int? combustivel, Guid? por) =>
            new(loja.Id, loja.Nome, EnderecoDto.De(loja.Endereco), loja.Telefone.Formatado, prevista, real, km, combustivel, por);

        return new LocacaoResponse(
            l.Id, l.Codigo, l.Status,
            new ClienteLocacaoResponse(cliente.Id, cliente.Nome, cliente.Cpf.Formatado, cliente.Email.Valor,
                cliente.Telefone.Formatado, $"{cliente.Cnh.Numero} ({cliente.Cnh.Categoria})"),
            new ReservadaPorResponse(reservadaPor.Id, reservadaPor.Nome, l.ReservadaPorPerfil, l.ReservadaPeloProprioCliente),
            new CarroLocacaoResponse(carro.Id, carro.Placa.Formatada, carro.Marca, carro.Modelo, carro.Cor, carro.Categoria, carro.Cambio),
            Movimento(unidades[l.LojaRetiradaId], l.DataRetiradaPrevista, l.DataRetiradaReal, l.KmRetirada,
                l.CombustivelRetirada, l.RetiradaRegistradaPorId),
            Movimento(unidades[l.LojaDevolucaoId], l.DataDevolucaoPrevista, l.DataDevolucaoReal, l.KmDevolucao,
                l.CombustivelDevolucao, l.DevolucaoRegistradaPorId),
            l.Protecao,
            l.Adicionais.Select(a => new AdicionalResponse(a.Tipo, a.Quantidade, a.ValorDiario)).ToList(),
            new ValoresLocacaoResponse(l.QuantidadeDiarias, l.ValorDiaria, l.ValorDiarias, l.ValorProtecao,
                l.ValorAdicionais, l.TaxaDevolucaoOutraLoja, l.ValorTotalPrevisto, l.QuantidadeDiariasExtras,
                l.ValorDiariasExtras, l.TaxaCombustivel, l.ValorDanos, l.ValorTotal),
            new PagamentoResponse(l.FormaPagamento, l.StatusPagamento, l.ValorPago, l.ValorReembolsado, l.SaldoDevedor,
                l.DataUltimoPagamento, l.DataUltimoReembolso),
            new CaucaoResponse(l.ValorCaucao, l.StatusCaucao, l.ValorCaucaoRetido),
            l.KmPercorridos,
            l.Status == StatusLocacao.Reservada && l.CancelamentoComReembolsoGarantido(relogio.Agora),
            l.CanceladaEm is null ? null : new CancelamentoResponse(l.CanceladaEm.Value, l.CanceladaPorId, l.MotivoCancelamento),
            l.Observacoes,
            l.CriadoEm,
            l.AtualizadoEm);
    }

    private async Task<DadosReserva> MontarDadosReservaAsync(Cliente cliente, ReservaRequest request, CancellationToken ct)
    {
        var carro = await CarregarCarroAsync(request.CarroId, ct);
        var (retirada, devolucao) = await CarregarLojasAsync(request.LojaRetiradaId, request.LojaDevolucaoId, ct);
        return new DadosReserva(cliente, carro, retirada, devolucao, request.DataRetirada, request.DataDevolucao,
            request.Protecao, CriarAdicionais(request.Adicionais), request.FormaPagamento, request.Observacoes);
    }

    private async Task GarantirSemConflitoAsync(DadosReserva dados, Guid? ignorarId, CancellationToken ct)
    {
        if (await locacoes.ExisteConflitoAsync(dados.Carro.Id, dados.DataRetirada, dados.DataDevolucao, ignorarId, ct))
            throw new ConflitoException("O carro já está reservado em parte deste período. Consulte os carros disponíveis.");
    }

    private static IReadOnlyCollection<AdicionalLocacao> CriarAdicionais(IReadOnlyList<AdicionalRequest>? adicionais) =>
        (adicionais ?? []).Select(a => AdicionalLocacao.Criar(a.Tipo, a.Quantidade)).ToList();

    private async Task<(Loja Retirada, Loja Devolucao)> CarregarLojasAsync(Guid retiradaId, Guid devolucaoId, CancellationToken ct)
    {
        var unidades = await lojas.ObterPorIdsAsync([retiradaId, devolucaoId], ct);
        return (unidades.GetValueOrDefault(retiradaId) ?? throw new RecursoNaoEncontradoException("Loja de retirada"),
            unidades.GetValueOrDefault(devolucaoId) ?? throw new RecursoNaoEncontradoException("Loja de devolução"));
    }

    private async Task<Carro> CarregarCarroAsync(Guid id, CancellationToken ct) =>
        await carros.ObterPorIdAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Carro");

    private async Task<Usuario> CarregarSolicitanteAsync(CancellationToken ct)
    {
        var usuario = await usuarios.ObterPorIdAsync(usuarioAtual.Id, ct);
        if (usuario is null || !usuario.Ativo)
            throw new CredenciaisInvalidasException("Usuário inexistente ou inativo.");
        return usuario;
    }

    /// <summary>Clientes só acessam as próprias locações; para os demais, a locação "não existe".</summary>
    private async Task<Locacao> CarregarAsync(Guid id, CancellationToken ct)
    {
        var locacao = await locacoes.ObterPorIdAsync(id, ct);
        if (locacao is null || (!usuarioAtual.EhEquipe && locacao.ClienteId != usuarioAtual.Id))
            throw new RecursoNaoEncontradoException("Locação");
        return locacao;
    }
}
