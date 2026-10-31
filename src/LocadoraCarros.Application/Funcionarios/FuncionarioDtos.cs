using System.ComponentModel.DataAnnotations;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Domain.Usuarios;

namespace LocadoraCarros.Application.Funcionarios;

public sealed record CriarFuncionarioRequest(
    [Required, StringLength(150)] string Nome,
    [Required] string Cpf,
    [Required, EmailAddress] string Email,
    [Required] string Telefone,
    [Required] string Senha,
    [Required, StringLength(20)] string Matricula,
    [Required, StringLength(80)] string Cargo,
    Guid LojaId,
    DateOnly DataAdmissao,
    PerfilUsuario Perfil = PerfilUsuario.Funcionario); // Funcionario ou Gerente

public sealed record AtualizarFuncionarioRequest(
    [Required, StringLength(150)] string Nome,
    [Required] string Cpf,
    [Required, EmailAddress] string Email,
    [Required] string Telefone,
    [Required, StringLength(20)] string Matricula,
    [Required, StringLength(80)] string Cargo,
    Guid LojaId,
    DateOnly DataAdmissao);

public sealed record FiltroFuncionariosRequest : PaginacaoRequest
{
    public Guid? LojaId { get; init; }
    /// <summary>Nome, CPF, e-mail ou matrícula.</summary>
    public string? Busca { get; init; }
    public bool IncluirInativos { get; init; }
}

public sealed record FuncionarioResponse(
    Guid Id,
    string Nome,
    string Cpf,
    string Email,
    string Telefone,
    PerfilUsuario Perfil,
    string Matricula,
    string Cargo,
    Guid LojaId,
    DateOnly DataAdmissao,
    bool Ativo,
    DateTime CriadoEm,
    DateTime? UltimoAcessoEm)
{
    public static FuncionarioResponse De(Funcionario f) => new(
        f.Id, f.Nome, f.Cpf.Formatado, f.Email.Valor, f.Telefone.Formatado, f.Perfil, f.Matricula, f.Cargo,
        f.LojaId, f.DataAdmissao, f.Ativo, f.CriadoEm, f.UltimoAcessoEm);
}
