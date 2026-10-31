using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Domain.Usuarios;

public class Funcionario : Usuario
{
    protected Funcionario() { }

    protected Funcionario(string nome, Cpf cpf, Email email, Telefone telefone, string senhaHash, PerfilUsuario perfil)
        : base(nome, cpf, email, telefone, senhaHash, perfil) { }

    public string Matricula { get; private set; } = null!;
    public string Cargo { get; private set; } = null!;
    public Guid LojaId { get; private set; }
    public DateOnly DataAdmissao { get; private set; }

    public static Funcionario Criar(string nome, Cpf cpf, Email email, Telefone telefone, string senhaHash,
        string matricula, string cargo, Guid lojaId, DateOnly dataAdmissao)
    {
        var funcionario = new Funcionario(nome, cpf, email, telefone, senhaHash, PerfilUsuario.Funcionario);
        funcionario.DefinirDadosFuncionais(matricula, cargo, lojaId, dataAdmissao);
        return funcionario;
    }

    /// <summary>Alteração cadastral completa: exclusiva do gerente.</summary>
    public void AtualizarCadastro(string nome, Email email, Telefone telefone, string matricula, string cargo,
        Guid lojaId, DateOnly dataAdmissao)
    {
        DefinirDadosBasicos(nome, email, telefone);
        DefinirDadosFuncionais(matricula, cargo, lojaId, dataAdmissao);
    }

    protected void DefinirDadosFuncionais(string matricula, string cargo, Guid lojaId, DateOnly dataAdmissao)
    {
        Guard.Garantir(lojaId != Guid.Empty, "Informe a loja de lotação.");
        Matricula = Guard.Texto(matricula, "Matrícula", 20).ToUpperInvariant();
        Cargo = Guard.Texto(cargo, "Cargo", 80);
        LojaId = lojaId;
        DataAdmissao = dataAdmissao;
    }
}

public sealed class Gerente : Funcionario
{
    private Gerente() { }

    private Gerente(string nome, Cpf cpf, Email email, Telefone telefone, string senhaHash)
        : base(nome, cpf, email, telefone, senhaHash, PerfilUsuario.Gerente) { }

    public static new Gerente Criar(string nome, Cpf cpf, Email email, Telefone telefone, string senhaHash,
        string matricula, string cargo, Guid lojaId, DateOnly dataAdmissao)
    {
        var gerente = new Gerente(nome, cpf, email, telefone, senhaHash);
        gerente.DefinirDadosFuncionais(matricula, cargo, lojaId, dataAdmissao);
        return gerente;
    }
}
