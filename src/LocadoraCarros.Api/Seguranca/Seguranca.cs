using System.Security.Claims;
using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LocadoraCarros.Api.Seguranca;

public static class Politicas
{
    /// <summary>Funcionários e gerentes.</summary>
    public const string Equipe = nameof(Equipe);

    /// <summary>Somente gerentes.</summary>
    public const string Gerente = nameof(Gerente);

    public static IServiceCollection AddPoliticasDeAcesso(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Equipe, p => p.RequireRole(nameof(PerfilUsuario.Funcionario), nameof(PerfilUsuario.Gerente)))
            .AddPolicy(Gerente, p => p.RequireRole(nameof(PerfilUsuario.Gerente)))
            // Seguro por padrão: todo endpoint exige bearer token, exceto os marcados com [AllowAnonymous].
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }
}

internal sealed class UsuarioAtual(IHttpContextAccessor accessor) : IUsuarioAtual
{
    private ClaimsPrincipal? Usuario => accessor.HttpContext?.User;

    public bool Autenticado => Usuario?.Identity?.IsAuthenticated == true;

    public Guid Id => Guid.TryParse(Usuario?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : Guid.Empty;

    // Sem perfil válido no token, assume o de menor privilégio.
    public PerfilUsuario Perfil =>
        Enum.TryParse<PerfilUsuario>(Usuario?.FindFirstValue(JwtOptions.ClaimPerfil), out var perfil) ? perfil : PerfilUsuario.Cliente;
}
