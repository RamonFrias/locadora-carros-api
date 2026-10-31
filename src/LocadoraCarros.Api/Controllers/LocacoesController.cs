using LocadoraCarros.Api.Seguranca;
using LocadoraCarros.Application.Locacoes;
using LocadoraCarros.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraCarros.Api.Controllers;

/// <summary>
/// Locações. Clientes reservam, alteram e cancelam as próprias; a equipe opera qualquer uma
/// e registra retirada, devolução e pagamentos no balcão.
/// </summary>
[ApiController]
[Route("api/locacoes")]
public sealed class LocacoesController(LocacaoService locacoes) : ControllerBase
{
    /// <summary>Simula valores (diárias, proteção, adicionais, taxa de devolução e caução) sem reservar.</summary>
    [HttpPost("cotacao")]
    public async Task<OrcamentoResponse> Cotar(CotacaoRequest request, CancellationToken ct) =>
        await locacoes.CotarAsync(request, ct);

    /// <summary>Cria a reserva. Para a equipe, ClienteId é obrigatório; o cliente reserva para si.</summary>
    [HttpPost]
    [ProducesResponseType<LocacaoResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(ReservaRequest request, CancellationToken ct)
    {
        var locacao = await locacoes.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = locacao.Id }, locacao);
    }

    /// <summary>Clientes veem apenas as próprias locações.</summary>
    [HttpGet]
    public async Task<ResultadoPaginado<LocacaoResumoResponse>> Listar([FromQuery] FiltroLocacoesRequest filtro, CancellationToken ct) =>
        await locacoes.ListarAsync(filtro, ct);

    [HttpGet("{id:guid}")]
    public async Task<LocacaoResponse> Obter(Guid id, CancellationToken ct) => await locacoes.ObterAsync(id, ct);

    /// <summary>Altera datas, lojas, carro, proteção, adicionais ou forma de pagamento (antes da retirada).</summary>
    [HttpPut("{id:guid}")]
    public async Task<LocacaoResponse> Alterar(Guid id, ReservaRequest request, CancellationToken ct) =>
        await locacoes.AlterarAsync(id, request, ct);

    /// <summary>Cancela. Reembolso integral com 24h ou mais de antecedência da retirada.</summary>
    [HttpPost("{id:guid}/cancelamento")]
    public async Task<LocacaoResponse> Cancelar(Guid id, CancelarLocacaoRequest request, CancellationToken ct) =>
        await locacoes.CancelarAsync(id, request, ct);

    [HttpPost("{id:guid}/retirada")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<LocacaoResponse> RegistrarRetirada(Guid id, RetiradaRequest request, CancellationToken ct) =>
        await locacoes.RegistrarRetiradaAsync(id, request, ct);

    /// <summary>Apura atraso, combustível e danos; encargos são descontados da caução.</summary>
    [HttpPost("{id:guid}/devolucao")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<LocacaoResponse> RegistrarDevolucao(Guid id, DevolucaoRequest request, CancellationToken ct) =>
        await locacoes.RegistrarDevolucaoAsync(id, request, ct);

    /// <summary>Quita no balcão o saldo devedor restante.</summary>
    [HttpPost("{id:guid}/quitacao")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<LocacaoResponse> QuitarSaldo(Guid id, CancellationToken ct) =>
        await locacoes.QuitarSaldoAsync(id, ct);
}
