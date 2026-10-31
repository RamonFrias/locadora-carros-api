using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Application.Clientes;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Application.Funcionarios;

/// <summary>Gestão da equipe: exclusiva do gerente.</summary>
public sealed class FuncionarioService(
    IUsuarioRepository usuarios,
    ILojaRepository lojas,
    ClienteService clientes,
    IPasswordHasher hasher,
    IUnitOfWork uow,
    IUsuarioAtual usuarioAtual)
{
    public async Task<FuncionarioResponse> CriarAsync(CriarFuncionarioRequest request, CancellationToken ct)
    {
        if (request.Perfil is not (PerfilUsuario.Funcionario or PerfilUsuario.Gerente))
            throw new ValidacaoException("Perfil deve ser Funcionario ou Gerente.");

        var cpf = Cpf.Criar(request.Cpf);
        var email = Email.Criar(request.Email);
        await clientes.GarantirUnicidadeAsync(cpf, email, null, ct);
        await GarantirMatriculaUnicaAsync(request.Matricula, null, ct);
        await GarantirLojaAtivaAsync(request.LojaId, ct);
        PoliticaSenha.Validar(request.Senha);

        var telefone = Telefone.Criar(request.Telefone);
        var senhaHash = hasher.Gerar(request.Senha);
        Funcionario funcionario = request.Perfil == PerfilUsuario.Gerente
            ? Gerente.Criar(request.Nome, cpf, email, telefone, senhaHash, request.Matricula, request.Cargo, request.LojaId, request.DataAdmissao)
            : Funcionario.Criar(request.Nome, cpf, email, telefone, senhaHash, request.Matricula, request.Cargo, request.LojaId, request.DataAdmissao);

        usuarios.Adicionar(funcionario);
        await uow.SalvarAsync(ct);
        return FuncionarioResponse.De(funcionario);
    }

    public async Task<ResultadoPaginado<FuncionarioResponse>> ListarAsync(FiltroFuncionariosRequest filtro, CancellationToken ct) =>
        (await usuarios.ListarFuncionariosAsync(filtro.LojaId, filtro.Busca, filtro.IncluirInativos, filtro.Pagina,
            filtro.TamanhoPagina, ct)).Mapear(FuncionarioResponse.De);

    public async Task<FuncionarioResponse> ObterAsync(Guid id, CancellationToken ct) =>
        FuncionarioResponse.De(await CarregarAsync(id, ct));

    public async Task<FuncionarioResponse> AtualizarAsync(Guid id, AtualizarFuncionarioRequest request, CancellationToken ct)
    {
        var funcionario = await CarregarAsync(id, ct);
        var cpf = Cpf.Criar(request.Cpf);
        var email = Email.Criar(request.Email);
        await clientes.GarantirUnicidadeAsync(cpf, email, id, ct);
        await GarantirMatriculaUnicaAsync(request.Matricula, id, ct);
        if (request.LojaId != funcionario.LojaId)
            await GarantirLojaAtivaAsync(request.LojaId, ct);

        funcionario.CorrigirCpf(cpf);
        funcionario.AtualizarCadastro(request.Nome, email, Telefone.Criar(request.Telefone), request.Matricula,
            request.Cargo, request.LojaId, request.DataAdmissao);
        await uow.SalvarAsync(ct);
        return FuncionarioResponse.De(funcionario);
    }

    public async Task RedefinirSenhaAsync(Guid id, RedefinirSenhaRequest request, CancellationToken ct)
    {
        var funcionario = await CarregarAsync(id, ct);
        PoliticaSenha.Validar(request.NovaSenha);
        funcionario.AlterarSenha(hasher.Gerar(request.NovaSenha));
        await uow.SalvarAsync(ct);
    }

    /// <summary>Exclusão lógica: preserva o histórico de quem registrou cada locação.</summary>
    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        if (id == usuarioAtual.Id)
            throw new ConflitoException("Você não pode excluir o próprio cadastro.");
        var funcionario = await CarregarAsync(id, ct);
        funcionario.Desativar();
        await uow.SalvarAsync(ct);
    }

    public async Task ReativarAsync(Guid id, CancellationToken ct)
    {
        var funcionario = await CarregarAsync(id, ct);
        funcionario.Reativar();
        await uow.SalvarAsync(ct);
    }

    private async Task GarantirMatriculaUnicaAsync(string matricula, Guid? ignorarId, CancellationToken ct)
    {
        if (await usuarios.ExisteMatriculaAsync(matricula.Trim().ToUpperInvariant(), ignorarId, ct))
            throw new ConflitoException("Já existe um funcionário com esta matrícula.");
    }

    private async Task GarantirLojaAtivaAsync(Guid lojaId, CancellationToken ct)
    {
        var loja = await lojas.ObterPorIdAsync(lojaId, ct) ?? throw new RecursoNaoEncontradoException("Loja");
        if (!loja.Ativa) throw new ValidacaoException("Não é possível lotar funcionários em uma loja inativa.");
    }

    private async Task<Funcionario> CarregarAsync(Guid id, CancellationToken ct) =>
        await usuarios.ObterFuncionarioAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Funcionário");
}
