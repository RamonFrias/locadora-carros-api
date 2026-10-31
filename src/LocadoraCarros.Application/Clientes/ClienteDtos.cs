using System.ComponentModel.DataAnnotations;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Domain.Usuarios;

namespace LocadoraCarros.Application.Clientes;

/// <summary>Cadastro de cliente (auto-cadastro ou feito pela equipe).</summary>
public sealed record CriarClienteRequest(
    [Required, StringLength(150)] string Nome,
    [Required] string Cpf,
    [Required, EmailAddress] string Email,
    [Required] string Telefone,
    DateOnly DataNascimento,
    [Required] EnderecoDto Endereco,
    [Required] CnhDto Cnh,
    [Required] string Senha);

/// <summary>Alteração feita pelo próprio cliente: todos os dados, exceto o CPF.</summary>
public sealed record AtualizarMeuCadastroRequest(
    [Required, StringLength(150)] string Nome,
    [Required, EmailAddress] string Email,
    [Required] string Telefone,
    DateOnly DataNascimento,
    [Required] EnderecoDto Endereco,
    [Required] CnhDto Cnh);

/// <summary>Alteração feita pela equipe: inclui correção de CPF.</summary>
public sealed record AtualizarClienteRequest(
    [Required, StringLength(150)] string Nome,
    [Required] string Cpf,
    [Required, EmailAddress] string Email,
    [Required] string Telefone,
    DateOnly DataNascimento,
    [Required] EnderecoDto Endereco,
    [Required] CnhDto Cnh);

public sealed record AlterarSenhaRequest(
    [Required] string SenhaAtual,
    [Required] string NovaSenha);

public sealed record RedefinirSenhaRequest([Required] string NovaSenha);

public sealed record FiltroClientesRequest : PaginacaoRequest
{
    /// <summary>Nome, CPF ou e-mail.</summary>
    public string? Busca { get; init; }
    public bool IncluirInativos { get; init; }
}

public sealed record ClienteResponse(
    Guid Id,
    string Nome,
    string Cpf,
    string Email,
    string Telefone,
    DateOnly DataNascimento,
    EnderecoDto Endereco,
    CnhDto Cnh,
    bool Ativo,
    DateTime CriadoEm,
    DateTime? UltimoAcessoEm)
{
    public PerfilUsuario Perfil => PerfilUsuario.Cliente;

    public static ClienteResponse De(Cliente c) => new(
        c.Id, c.Nome, c.Cpf.Formatado, c.Email.Valor, c.Telefone.Formatado, c.DataNascimento,
        EnderecoDto.De(c.Endereco), CnhDto.De(c.Cnh), c.Ativo, c.CriadoEm, c.UltimoAcessoEm);
}
