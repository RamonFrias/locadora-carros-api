using System.Security.Claims;
using System.Text;
using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Application.Auth;
using LocadoraCarros.Domain.Usuarios;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LocadoraCarros.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Secao = "Jwt";
    public const string ClaimPerfil = "role";
    public const string ClaimNome = "name";

    public string Emissor { get; init; } = "LocadoraCarros.Api";
    public string Audiencia { get; init; } = "LocadoraCarros.Clientes";
    /// <summary>Chave HMAC-SHA256 com pelo menos 32 bytes. Nunca versionar a chave de produção.</summary>
    public string ChaveSecreta { get; init; } = string.Empty;
    public int ExpiracaoMinutos { get; init; } = 60;

    public SymmetricSecurityKey ObterChave()
    {
        var bytes = Encoding.UTF8.GetBytes(ChaveSecreta);
        if (bytes.Length < 32)
            throw new InvalidOperationException(
                "Configure 'Jwt:ChaveSecreta' com pelo menos 32 caracteres (user-secrets ou variável de ambiente Jwt__ChaveSecreta).");
        return new SymmetricSecurityKey(bytes);
    }
}

internal sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _opcoes = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenResponse Gerar(Usuario usuario)
    {
        var agora = DateTime.UtcNow;
        var expiraEm = agora.AddMinutes(_opcoes.ExpiracaoMinutos);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = _opcoes.Emissor,
            Audience = _opcoes.Audiencia,
            IssuedAt = agora,
            NotBefore = agora,
            Expires = expiraEm,
            SigningCredentials = new SigningCredentials(_opcoes.ObterChave(), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtOptions.ClaimNome, usuario.Nome),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email.Valor),
                new Claim(JwtOptions.ClaimPerfil, usuario.Perfil.ToString())
            ])
        };

        return new TokenResponse(
            _handler.CreateToken(descritor),
            "Bearer",
            expiraEm,
            _opcoes.ExpiracaoMinutos * 60,
            new UsuarioLogadoResponse(usuario.Id, usuario.Nome, usuario.Email.Valor, usuario.Perfil));
    }
}
