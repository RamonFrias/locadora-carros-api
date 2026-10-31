using LocadoraCarros.Api.Seguranca;
using LocadoraCarros.Application.Carros;
using LocadoraCarros.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraCarros.Api.Controllers;

/// <summary>
/// Frota. Clientes só consultam os carros disponíveis de uma loja; a frota completa,
/// o detalhe e a manutenção são exclusivos da equipe.
/// </summary>
[ApiController]
[Route("api/carros")]
public sealed class CarrosController(CarroService carros) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<ResultadoPaginado<CarroResponse>> Listar([FromQuery] FiltroCarrosRequest filtro, CancellationToken ct) =>
        await carros.ListarAsync(filtro, ct);

    /// <summary>Carros da loja livres para reserva no período (datas no horário local, sem fuso). Qualquer usuário logado.</summary>
    [HttpGet("disponiveis")]
    public async Task<IReadOnlyList<CarroDisponivelResponse>> Disponiveis([FromQuery] DisponibilidadeRequest request, CancellationToken ct) =>
        await carros.ListarDisponiveisAsync(request, ct);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<CarroResponse> Obter(Guid id, CancellationToken ct) => await carros.ObterAsync(id, ct);

    [HttpPost]
    [Authorize(Policy = Politicas.Equipe)]
    [ProducesResponseType<CarroResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarCarroRequest request, CancellationToken ct)
    {
        var carro = await carros.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = carro.Id }, carro);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<CarroResponse> Atualizar(Guid id, AtualizarCarroRequest request, CancellationToken ct) =>
        await carros.AtualizarAsync(id, request, ct);

    /// <summary>Manutenção, limpeza, transferência, indisponível... ("Alugado" é automático).</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<CarroResponse> AlterarStatus(Guid id, AlterarStatusCarroRequest request, CancellationToken ct) =>
        await carros.AlterarStatusAsync(id, request, ct);

    [HttpPost("{id:guid}/transferencia")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<CarroResponse> Transferir(Guid id, TransferirCarroRequest request, CancellationToken ct) =>
        await carros.TransferirAsync(id, request, ct);

    /// <summary>Exclusão lógica: o carro sai da frota (status Inativo).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Politicas.Equipe)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await carros.ExcluirAsync(id, ct);
        return NoContent();
    }
}
