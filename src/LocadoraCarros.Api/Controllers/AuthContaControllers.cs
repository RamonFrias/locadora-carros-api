using LocadoraCarros.Application.Auth;
using LocadoraCarros.Application.Clientes;
using LocadoraCarros.Application.Contas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LocadoraCarros.Api.Controllers;

/// <summary>Autenticação (bearer token) e auto-cadastro de clientes.</summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting(Program.LimiteAutenticacao)]
public sealed class AuthController(AuthService auth, ClienteService clientes) : ControllerBase
{
    /// <summary>Gera o bearer token. O login pode ser o e-mail ou o CPF.</summary>
    [HttpPost("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<TokenResponse> Login(LoginRequest request, CancellationToken ct) =>
        await auth.LoginAsync(request, ct);

    /// <summary>Cadastro público de um novo cliente.</summary>
    [HttpPost("registro")]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Registrar(CriarClienteRequest request, CancellationToken ct)
    {
        var cliente = await clientes.CriarAsync(request, ct);
        return Created("/api/conta", cliente);
    }
}

/// <summary>Cadastro do usuário logado.</summary>
[ApiController]
[Route("api/conta")]
public sealed class ContaController(ContaService conta) : ControllerBase
{
    /// <summary>Dados do usuário logado (cliente ou funcionário).</summary>
    [HttpGet]
    public async Task<object> Obter(CancellationToken ct) => await conta.ObterAsync(ct);

    /// <summary>Clientes alteram todos os dados, exceto o CPF. Funcionários recebem 403 (só podem trocar a senha).</summary>
    [HttpPut]
    [ProducesResponseType<ClienteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ClienteResponse> Atualizar(AtualizarMeuCadastroRequest request, CancellationToken ct) =>
        await conta.AtualizarAsync(request, ct);

    /// <summary>Troca de senha (todos os perfis).</summary>
    [HttpPut("senha")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaRequest request, CancellationToken ct)
    {
        await conta.AlterarSenhaAsync(request, ct);
        return NoContent();
    }
}
