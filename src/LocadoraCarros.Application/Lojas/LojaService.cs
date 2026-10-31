using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Application.Locacoes;
using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Application.Lojas;

public sealed class LojaService(
    ILojaRepository lojas,
    ICarroRepository carros,
    ILocacaoRepository locacoes,
    IUsuarioRepository usuarios,
    LocacaoService locacaoService,
    IUnitOfWork uow,
    IRelogio relogio)
{
    private const int LimitePainel = 100;

    public async Task<IReadOnlyList<LojaResponse>> ListarAsync(FiltroLojasRequest filtro, CancellationToken ct) =>
        (await lojas.ListarAsync(filtro.Cidade, filtro.Uf, filtro.IncluirInativas, ct)).Select(LojaResponse.De).ToList();

    public async Task<LojaResponse> ObterAsync(Guid id, CancellationToken ct) =>
        LojaResponse.De(await CarregarAsync(id, ct));

    public async Task<LojaResponse> CriarAsync(LojaRequest request, CancellationToken ct)
    {
        var cnpj = Cnpj.Criar(request.Cnpj);
        await GarantirUnicidadeAsync(request.Codigo, cnpj, null, ct);

        var loja = Loja.Criar(request.Codigo, request.Nome, cnpj, request.Tipo, request.Endereco.ParaDominio(),
            Telefone.Criar(request.Telefone), Email.Criar(request.Email), request.Funciona24Horas,
            HorarioObrigatorio(request.HorarioAbertura, request.Funciona24Horas),
            HorarioObrigatorio(request.HorarioFechamento, request.Funciona24Horas),
            request.AceitaDevolucaoDeOutrasLojas);

        lojas.Adicionar(loja);
        await uow.SalvarAsync(ct);
        return LojaResponse.De(loja);
    }

    /// <summary>O código da loja é fixo; os demais dados podem ser alterados.</summary>
    public async Task<LojaResponse> AtualizarAsync(Guid id, LojaRequest request, CancellationToken ct)
    {
        var loja = await CarregarAsync(id, ct);
        var cnpj = Cnpj.Criar(request.Cnpj);
        await GarantirUnicidadeAsync(loja.Codigo, cnpj, id, ct);

        loja.Atualizar(request.Nome, cnpj, request.Tipo, request.Endereco.ParaDominio(), Telefone.Criar(request.Telefone),
            Email.Criar(request.Email), request.Funciona24Horas,
            HorarioObrigatorio(request.HorarioAbertura, request.Funciona24Horas),
            HorarioObrigatorio(request.HorarioFechamento, request.Funciona24Horas),
            request.AceitaDevolucaoDeOutrasLojas);
        await uow.SalvarAsync(ct);
        return LojaResponse.De(loja);
    }

    /// <summary>Desativa a loja. Exige que ela não tenha frota, equipe ou locações ativas.</summary>
    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        var loja = await CarregarAsync(id, ct);
        if (await locacoes.LojaPossuiLocacoesAtivasAsync(id, ct))
            throw new ConflitoException("A loja possui locações com retirada ou devolução pendentes.");
        var frota = await carros.ContarPorStatusAsync(id, ct);
        if (frota.Where(f => f.Key != StatusCarro.Inativo).Sum(f => f.Value) > 0)
            throw new ConflitoException("Transfira ou exclua os carros da loja antes de desativá-la.");
        if (await usuarios.LojaPossuiFuncionariosAtivosAsync(id, ct))
            throw new ConflitoException("Transfira os funcionários da loja antes de desativá-la.");

        loja.Desativar();
        await uow.SalvarAsync(ct);
    }

    public async Task ReativarAsync(Guid id, CancellationToken ct)
    {
        var loja = await CarregarAsync(id, ct);
        loja.Reativar();
        await uow.SalvarAsync(ct);
    }

    public async Task<PainelLojaResponse> ObterPainelAsync(Guid id, DateOnly? data, CancellationToken ct)
    {
        var loja = await CarregarAsync(id, ct);
        var dia = data ?? relogio.Hoje;
        var inicio = dia.ToDateTime(TimeOnly.MinValue);
        var fim = inicio.AddDays(1);

        var frota = await carros.ContarPorStatusAsync(id, ct);

        var retiradas = await locacoes.ListarAsync(new FiltroLocacoes(LojaId: id, Movimento: TipoMovimentoLoja.Retirada,
            De: inicio, Ate: fim), 1, LimitePainel, ct);
        var devolucoes = await locacoes.ListarAsync(new FiltroLocacoes(LojaId: id, Movimento: TipoMovimentoLoja.Devolucao,
            De: inicio, Ate: fim), 1, LimitePainel, ct);
        var atrasadas = await locacoes.ListarAsync(new FiltroLocacoes(LojaId: id, Movimento: TipoMovimentoLoja.Devolucao,
            Status: StatusLocacao.EmAndamento, Ate: relogio.Agora - TabelaPrecos.ToleranciaAtraso), 1, LimitePainel, ct);
        var emAndamento = await locacoes.ListarAsync(new FiltroLocacoes(LojaId: id, Movimento: TipoMovimentoLoja.Devolucao,
            Status: StatusLocacao.EmAndamento), 1, 1, ct);

        return new PainelLojaResponse(
            LojaResponse.De(loja),
            dia,
            frota.Where(f => f.Key != StatusCarro.Inativo).Sum(f => f.Value),
            frota,
            await locacaoService.MontarResumosAsync(retiradas.Itens.Where(l => l.Status != StatusLocacao.Cancelada).ToList(), ct),
            await locacaoService.MontarResumosAsync(devolucoes.Itens.Where(l => l.Status != StatusLocacao.Cancelada).ToList(), ct),
            await locacaoService.MontarResumosAsync(atrasadas.Itens, ct),
            emAndamento.TotalItens);
    }

    private async Task GarantirUnicidadeAsync(string codigo, Cnpj cnpj, Guid? ignorarId, CancellationToken ct)
    {
        if (await lojas.ExisteCodigoAsync(codigo.Trim().ToUpperInvariant(), ignorarId, ct))
            throw new ConflitoException("Já existe uma loja com este código.");
        if (await lojas.ExisteCnpjAsync(cnpj, ignorarId, ct))
            throw new ConflitoException("Já existe uma loja com este CNPJ.");
    }

    private static TimeOnly HorarioObrigatorio(TimeOnly? horario, bool funciona24Horas) =>
        funciona24Horas ? TimeOnly.MinValue
            : horario ?? throw new ValidacaoException("Informe os horários de abertura e fechamento.");

    private async Task<Loja> CarregarAsync(Guid id, CancellationToken ct) =>
        await lojas.ObterPorIdAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Loja");
}
