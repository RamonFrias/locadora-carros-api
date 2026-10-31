using LocadoraCarros.Application.Auth;
using LocadoraCarros.Domain.Usuarios;

namespace LocadoraCarros.Application.Abstractions;

/// <summary>Usuário autenticado na requisição atual (extraído do bearer token).</summary>
public interface IUsuarioAtual
{
    bool Autenticado { get; }
    Guid Id { get; }
    PerfilUsuario Perfil { get; }
    bool EhEquipe => Perfil is PerfilUsuario.Funcionario or PerfilUsuario.Gerente;
}

public interface IPasswordHasher
{
    string Gerar(string senha);
    bool Verificar(string senha, string hash);
}

public interface ITokenService
{
    TokenResponse Gerar(Usuario usuario);
}

/// <summary>Relógio da aplicação no horário local das lojas (abstraído para testes).</summary>
public interface IRelogio
{
    DateTime Agora { get; }
    DateOnly Hoje => DateOnly.FromDateTime(Agora);
}
