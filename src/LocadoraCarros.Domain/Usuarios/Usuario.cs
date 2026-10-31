using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Domain.Usuarios;

public enum PerfilUsuario
{
    Cliente = 1,
    Funcionario = 2,
    Gerente = 3
}

public abstract class Usuario : AggregateRoot
{
    protected Usuario() { }

    protected Usuario(string nome, Cpf cpf, Email email, Telefone telefone, string senhaHash, PerfilUsuario perfil)
    {
        Cpf = cpf;
        Perfil = perfil;
        DefinirDadosBasicos(nome, email, telefone);
        AlterarSenha(senhaHash);
    }

    public string Nome { get; private set; } = null!;
    public Cpf Cpf { get; protected set; } = null!;
    public Email Email { get; private set; } = null!;
    public Telefone Telefone { get; private set; } = null!;
    public string SenhaHash { get; private set; } = null!;
    public PerfilUsuario Perfil { get; private set; }
    public bool Ativo { get; private set; } = true;
    public DateTime? UltimoAcessoEm { get; private set; }

    public bool EhEquipe => Perfil is PerfilUsuario.Funcionario or PerfilUsuario.Gerente;

    public void AlterarSenha(string novoHash) =>
        SenhaHash = Guard.Texto(novoHash, "Senha", 500);

    public void RegistrarAcesso(DateTime agora) => UltimoAcessoEm = agora;

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;

    /// <summary>Correção de CPF: só pode ser feita pela equipe (a aplicação valida quem solicita).</summary>
    public void CorrigirCpf(Cpf cpf) => Cpf = cpf;

    protected void DefinirDadosBasicos(string nome, Email email, Telefone telefone)
    {
        Nome = Guard.Texto(nome, "Nome");
        Email = email;
        Telefone = telefone;
    }
}
