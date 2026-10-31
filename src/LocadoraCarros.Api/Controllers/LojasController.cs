using LocadoraCarros.Api.Seguranca;
using LocadoraCarros.Application.Carros;
using LocadoraCarros.Application.Funcionarios;
using LocadoraCarros.Application.Locacoes;
using LocadoraCarros.Application.Lojas;
using LocadoraCarros.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraCarros.Api.Controllers;

/// <summary>Lojas. Consulta pública; cadastro pelo gerente; operação da loja pela equipe.</summary>
[ApiController]
[Route("api/lojas")]
public sealed class LojasController(
    LojaService lojas,
    CarroService carros,
    LocacaoService locacoes,
    FuncionarioService funcionarios) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IReadOnlyList<LojaResponse>> Listar([FromQuery] FiltroLojasRequest filtro, CancellationToken ct) =>
        await lojas.ListarAsync(filtro, ct);

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<LojaResponse> Obter(Guid id, CancellationToken ct) => await lojas.ObterAsync(id, ct);

    [HttpPost]
    [Authorize(Policy = Politicas.Gerente)]
    [ProducesResponseType<LojaResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(LojaRequest request, CancellationToken ct)
    {
        var loja = await lojas.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = loja.Id }, loja);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Politicas.Gerente)]
    public async Task<LojaResponse> Atualizar(Guid id, LojaRequest request, CancellationToken ct) =>
        await lojas.AtualizarAsync(id, request, ct);

    /// <summary>Desativa a loja (exige que esteja sem frota, equipe e locações ativas).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Politicas.Gerente)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await lojas.ExcluirAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reativacao")]
    [Authorize(Policy = Politicas.Gerente)]
    public async Task<IActionResult> Reativar(Guid id, CancellationToken ct)
    {
        await lojas.ReativarAsync(id, ct);
        return NoContent();
    }

    /// <summary>Carros cadastrados atualmente na loja.</summary>
    [HttpGet("{id:guid}/carros")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<ResultadoPaginado<CarroResponse>> Carros(Guid id, [FromQuery] FiltroCarrosRequest filtro, CancellationToken ct)
    {
        await lojas.ObterAsync(id, ct);
        return await carros.ListarAsync(filtro with { LojaId = id }, ct);
    }

    /// <summary>Locações que saem (Movimento=Retirada), chegam (Movimento=Devolucao) ou passam pela loja (Todos).</summary>
    [HttpGet("{id:guid}/locacoes")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<ResultadoPaginado<LocacaoResumoResponse>> Locacoes(Guid id, [FromQuery] FiltroLocacoesRequest filtro,
        CancellationToken ct)
    {
        await lojas.ObterAsync(id, ct);
        return await locacoes.ListarAsync(filtro with { LojaId = id }, ct);
    }

    [HttpGet("{id:guid}/funcionarios")]
    [Authorize(Policy = Politicas.Gerente)]
    public async Task<ResultadoPaginado<FuncionarioResponse>> Funcionarios(Guid id, [FromQuery] FiltroFuncionariosRequest filtro,
        CancellationToken ct)
    {
        await lojas.ObterAsync(id, ct);
        return await funcionarios.ListarAsync(filtro with { LojaId = id }, ct);
    }

    /// <summary>Painel do dia: frota por status, retiradas, devoluções e atrasos.</summary>
    [HttpGet("{id:guid}/painel")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<PainelLojaResponse> Painel(Guid id, [FromQuery] DateOnly? data, CancellationToken ct) =>
        await lojas.ObterPainelAsync(id, data, ct);
}
