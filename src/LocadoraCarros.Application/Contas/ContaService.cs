using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Application.Clientes;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Application.Funcionarios;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Application.Contas;

/// <summary>Operações do usuário logado sobre o próprio cadastro.</summary>
public sealed class ContaService(
    IUsuarioRepository usuarios,
    ClienteService clientes,
    IPasswordHasher hasher,
    IUnitOfWork uow,
    IRelogio relogio,
    IUsuarioAtual usuarioAtual)
{
    public async Task<object> ObterAsync(CancellationToken ct)
    {
        var usuario = await CarregarAsync(ct);
        return usuario switch
        {
            Cliente cliente => ClienteResponse.De(cliente),
            Funcionario funcionario => FuncionarioResponse.De(funcionario),
            _ => throw new InvalidOperationException("Perfil de usuário desconhecido.")
        };
    }

    /// <summary>Clientes alteram todos os dados, exceto o CPF. Funcionários e gerentes só alteram a senha.</summary>
    public async Task<ClienteResponse> AtualizarAsync(AtualizarMeuCadastroRequest request, CancellationToken ct)
    {
        if (await CarregarAsync(ct) is not Cliente cliente)
            throw new AcessoNegadoException(
                "Funcionários podem alterar apenas a própria senha. Os demais dados são alterados pelo gerente.");

        var email = Email.Criar(request.Email);
        await clientes.GarantirUnicidadeAsync(cliente.Cpf, email, cliente.Id, ct);

        cliente.AtualizarCadastro(request.Nome, email, Telefone.Criar(request.Telefone), request.DataNascimento,
            request.Endereco.ParaDominio(), request.Cnh.ParaDominio(), relogio.Hoje);
        await uow.SalvarAsync(ct);
        return ClienteResponse.De(cliente);
    }

    public async Task AlterarSenhaAsync(AlterarSenhaRequest request, CancellationToken ct)
    {
        var usuario = await CarregarAsync(ct);
        if (!hasher.Verificar(request.SenhaAtual, usuario.SenhaHash))
            throw new ValidacaoException("Senha atual incorreta.");
        if (request.SenhaAtual == request.NovaSenha)
            throw new ValidacaoException("A nova senha deve ser diferente da atual.");

        PoliticaSenha.Validar(request.NovaSenha);
        usuario.AlterarSenha(hasher.Gerar(request.NovaSenha));
        await uow.SalvarAsync(ct);
    }

    private async Task<Usuario> CarregarAsync(CancellationToken ct)
    {
        var usuario = await usuarios.ObterPorIdAsync(usuarioAtual.Id, ct);
        if (usuario is null || !usuario.Ativo)
            throw new CredenciaisInvalidasException("Usuário inexistente ou inativo.");
        return usuario;
    }
}
