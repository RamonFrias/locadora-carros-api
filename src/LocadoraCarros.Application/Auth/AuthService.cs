using System.ComponentModel.DataAnnotations;
using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Application.Auth;

public sealed record LoginRequest(
    [Required] string Login, // e-mail ou CPF
    [Required] string Senha);

public sealed record UsuarioLogadoResponse(Guid Id, string Nome, string Email, PerfilUsuario Perfil);

public sealed record TokenResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiraEmUtc,
    int ExpiraEmSegundos,
    UsuarioLogadoResponse Usuario);

public sealed class AuthService(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    ITokenService tokens,
    IUnitOfWork uow,
    IRelogio relogio)
{
    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var usuario = await BuscarPorLoginAsync(request.Login, ct);
        if (usuario is null || !hasher.Verificar(request.Senha, usuario.SenhaHash))
            throw new CredenciaisInvalidasException();
        if (!usuario.Ativo)
            throw new CredenciaisInvalidasException("Usuário inativo. Procure uma de nossas lojas.");

        usuario.RegistrarAcesso(relogio.Agora);
        await uow.SalvarAsync(ct);
        return tokens.Gerar(usuario);
    }

    private async Task<Usuario?> BuscarPorLoginAsync(string login, CancellationToken ct)
    {
        try
        {
            return login.Contains('@')
                ? await usuarios.ObterPorEmailAsync(Email.Criar(login), ct)
                : await usuarios.ObterPorCpfAsync(Cpf.Criar(login), ct);
        }
        catch (DomainException)
        {
            return null; // login em formato inválido é tratado como credencial inválida
        }
    }
}
