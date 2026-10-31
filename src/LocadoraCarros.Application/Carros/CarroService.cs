using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.Lojas;

namespace LocadoraCarros.Application.Carros;

public sealed class CarroService(
    ICarroRepository carros,
    ILojaRepository lojas,
    ILocacaoRepository locacoes,
    IUnitOfWork uow,
    IRelogio relogio)
{
    public async Task<ResultadoPaginado<CarroResponse>> ListarAsync(FiltroCarrosRequest filtro, CancellationToken ct) =>
        (await carros.ListarAsync(
            new FiltroCarros(filtro.LojaId, filtro.Categoria, filtro.Status, filtro.Busca, filtro.IncluirInativos),
            filtro.Pagina, filtro.TamanhoPagina, ct)).Mapear(CarroResponse.De);

    public async Task<CarroResponse> ObterAsync(Guid id, CancellationToken ct) =>
        CarroResponse.De(await CarregarAsync(id, ct));

    public async Task<IReadOnlyList<CarroDisponivelResponse>> ListarDisponiveisAsync(DisponibilidadeRequest request, CancellationToken ct)
    {
        if (request is not { LojaId: { } lojaId, Inicio: { } inicio, Fim: { } fim })
            throw new ValidacaoException("Informe a loja (lojaId), o início e o fim do período.");
        if (fim <= inicio)
            throw new ValidacaoException("O fim do período deve ser posterior ao início.");
        var loja = await lojas.ObterPorIdAsync(lojaId, ct) ?? throw new RecursoNaoEncontradoException("Loja");
        if (!loja.Ativa)
            throw new ValidacaoException("A loja informada está inativa.");

        var disponiveis = await carros.ListarDisponiveisAsync(lojaId, inicio, fim, request.Categoria, ct);
        return disponiveis.Select(CarroDisponivelResponse.De).ToList();
    }

    public async Task<CarroResponse> CriarAsync(CriarCarroRequest request, CancellationToken ct)
    {
        var dados = request.ParaDominio();
        await GarantirUnicidadeAsync(dados, null, ct);
        await GarantirLojaAtivaAsync(request.LojaId, ct);

        var carro = Carro.Criar(dados, request.LojaId, request.Quilometragem, relogio.Agora.Year);
        carros.Adicionar(carro);
        await uow.SalvarAsync(ct);
        return CarroResponse.De(carro);
    }

    public async Task<CarroResponse> AtualizarAsync(Guid id, AtualizarCarroRequest request, CancellationToken ct)
    {
        var carro = await CarregarAsync(id, ct);
        var dados = request.ParaDominio();
        await GarantirUnicidadeAsync(dados, id, ct);

        carro.Atualizar(dados, relogio.Agora.Year);
        await uow.SalvarAsync(ct);
        return CarroResponse.De(carro);
    }

    public async Task<CarroResponse> AlterarStatusAsync(Guid id, AlterarStatusCarroRequest request, CancellationToken ct)
    {
        var carro = await CarregarAsync(id, ct);
        carro.AlterarStatus(request.Status, request.Observacao);
        await uow.SalvarAsync(ct);
        return CarroResponse.De(carro);
    }

    public async Task<CarroResponse> TransferirAsync(Guid id, TransferirCarroRequest request, CancellationToken ct)
    {
        var carro = await CarregarAsync(id, ct);
        if (await locacoes.CarroPossuiLocacoesAtivasAsync(id, ct))
            throw new ConflitoException("O carro possui reservas na loja atual. Altere ou cancele-as antes de transferir.");
        await GarantirLojaAtivaAsync(request.LojaId, ct);

        carro.TransferirParaLoja(request.LojaId);
        await uow.SalvarAsync(ct);
        return CarroResponse.De(carro);
    }

    /// <summary>Exclusão lógica: o carro sai da frota, mas o histórico de locações é mantido.</summary>
    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        var carro = await CarregarAsync(id, ct);
        if (await locacoes.CarroPossuiLocacoesAtivasAsync(id, ct))
            throw new ConflitoException("O carro possui locações reservadas ou em andamento.");
        carro.Inativar();
        await uow.SalvarAsync(ct);
    }

    private async Task GarantirUnicidadeAsync(DadosCarro dados, Guid? ignorarId, CancellationToken ct)
    {
        if (await carros.ExistePlacaAsync(dados.Placa.EquivalenteMercosul, ignorarId, ct))
            throw new ConflitoException("Já existe um carro com esta placa (considerando o equivalente Mercosul).");
        if (await carros.ExisteRenavamAsync(new string(dados.Renavam.Where(char.IsDigit).ToArray()), ignorarId, ct))
            throw new ConflitoException("Já existe um carro com este RENAVAM.");
        if (await carros.ExisteChassiAsync(dados.Chassi.Trim().ToUpperInvariant(), ignorarId, ct))
            throw new ConflitoException("Já existe um carro com este chassi.");
    }

    private async Task GarantirLojaAtivaAsync(Guid lojaId, CancellationToken ct)
    {
        var loja = await lojas.ObterPorIdAsync(lojaId, ct) ?? throw new RecursoNaoEncontradoException("Loja");
        if (!loja.Ativa) throw new ValidacaoException("A loja informada está inativa.");
    }

    private async Task<Carro> CarregarAsync(Guid id, CancellationToken ct) =>
        await carros.ObterPorIdAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Carro");
}
