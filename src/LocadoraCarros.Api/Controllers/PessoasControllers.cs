using LocadoraCarros.Api.Seguranca;
using LocadoraCarros.Application.Clientes;
using LocadoraCarros.Application.Funcionarios;
using LocadoraCarros.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocadoraCarros.Api.Controllers;

/// <summary>Gestão de clientes pela equipe (funcionários e gerentes).</summary>
[ApiController]
[Route("api/clientes")]
[Authorize(Policy = Politicas.Equipe)]
public sealed class ClientesController(ClienteService clientes) : ControllerBase
{
    [HttpGet]
    public async Task<ResultadoPaginado<ClienteResponse>> Listar([FromQuery] FiltroClientesRequest filtro, CancellationToken ct) =>
        await clientes.ListarAsync(filtro, ct);

    [HttpGet("{id:guid}")]
    public async Task<ClienteResponse> Obter(Guid id, CancellationToken ct) => await clientes.ObterAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarClienteRequest request, CancellationToken ct)
    {
        var cliente = await clientes.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = cliente.Id }, cliente);
    }

    /// <summary>Atualização completa, inclusive correção de CPF.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ClienteResponse> Atualizar(Guid id, AtualizarClienteRequest request, CancellationToken ct) =>
        await clientes.AtualizarAsync(id, request, ct);

    [HttpPut("{id:guid}/senha")]
    public async Task<IActionResult> RedefinirSenha(Guid id, RedefinirSenhaRequest request, CancellationToken ct)
    {
        await clientes.RedefinirSenhaAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Exclusão lógica (o histórico de locações é preservado).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await clientes.ExcluirAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reativacao")]
    public async Task<IActionResult> Reativar(Guid id, CancellationToken ct)
    {
        await clientes.ReativarAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Gestão de funcionários e gerentes: exclusiva do gerente.</summary>
[ApiController]
[Route("api/funcionarios")]
[Authorize(Policy = Politicas.Gerente)]
public sealed class FuncionariosController(FuncionarioService funcionarios) : ControllerBase
{
    [HttpGet]
    public async Task<ResultadoPaginado<FuncionarioResponse>> Listar([FromQuery] FiltroFuncionariosRequest filtro, CancellationToken ct) =>
        await funcionarios.ListarAsync(filtro, ct);

    [HttpGet("{id:guid}")]
    public async Task<FuncionarioResponse> Obter(Guid id, CancellationToken ct) => await funcionarios.ObterAsync(id, ct);

    [HttpPost]
    [ProducesResponseType<FuncionarioResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar(CriarFuncionarioRequest request, CancellationToken ct)
    {
        var funcionario = await funcionarios.CriarAsync(request, ct);
        return CreatedAtAction(nameof(Obter), new { id = funcionario.Id }, funcionario);
    }

    [HttpPut("{id:guid}")]
    public async Task<FuncionarioResponse> Atualizar(Guid id, AtualizarFuncionarioRequest request, CancellationToken ct) =>
        await funcionarios.AtualizarAsync(id, request, ct);

    [HttpPut("{id:guid}/senha")]
    public async Task<IActionResult> RedefinirSenha(Guid id, RedefinirSenhaRequest request, CancellationToken ct)
    {
        await funcionarios.RedefinirSenhaAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
    {
        await funcionarios.ExcluirAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reativacao")]
    public async Task<IActionResult> Reativar(Guid id, CancellationToken ct)
    {
        await funcionarios.ReativarAsync(id, ct);
        return NoContent();
    }
}
