using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Application.Common;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Application.Clientes;

/// <summary>Gestão de clientes pela equipe e auto-cadastro.</summary>
public sealed class ClienteService(
    IUsuarioRepository usuarios,
    ILocacaoRepository locacoes,
    IPasswordHasher hasher,
    IUnitOfWork uow,
    IRelogio relogio)
{
    public async Task<ClienteResponse> CriarAsync(CriarClienteRequest request, CancellationToken ct)
    {
        var cpf = Cpf.Criar(request.Cpf);
        var email = Email.Criar(request.Email);
        await GarantirUnicidadeAsync(cpf, email, null, ct);
        PoliticaSenha.Validar(request.Senha);

        var cliente = Cliente.Criar(request.Nome, cpf, email, Telefone.Criar(request.Telefone),
            hasher.Gerar(request.Senha), request.DataNascimento, request.Endereco.ParaDominio(),
            request.Cnh.ParaDominio(), relogio.Hoje);

        usuarios.Adicionar(cliente);
        await uow.SalvarAsync(ct);
        return ClienteResponse.De(cliente);
    }

    public async Task<ResultadoPaginado<ClienteResponse>> ListarAsync(FiltroClientesRequest filtro, CancellationToken ct) =>
        (await usuarios.ListarClientesAsync(filtro.Busca, filtro.IncluirInativos, filtro.Pagina, filtro.TamanhoPagina, ct))
        .Mapear(ClienteResponse.De);

    public async Task<ClienteResponse> ObterAsync(Guid id, CancellationToken ct) =>
        ClienteResponse.De(await CarregarAsync(id, ct));

    public async Task<ClienteResponse> AtualizarAsync(Guid id, AtualizarClienteRequest request, CancellationToken ct)
    {
        var cliente = await CarregarAsync(id, ct);
        var cpf = Cpf.Criar(request.Cpf);
        var email = Email.Criar(request.Email);
        await GarantirUnicidadeAsync(cpf, email, id, ct);

        cliente.CorrigirCpf(cpf);
        cliente.AtualizarCadastro(request.Nome, email, Telefone.Criar(request.Telefone), request.DataNascimento,
            request.Endereco.ParaDominio(), request.Cnh.ParaDominio(), relogio.Hoje);

        await uow.SalvarAsync(ct);
        return ClienteResponse.De(cliente);
    }

    public async Task RedefinirSenhaAsync(Guid id, RedefinirSenhaRequest request, CancellationToken ct)
    {
        var cliente = await CarregarAsync(id, ct);
        PoliticaSenha.Validar(request.NovaSenha);
        cliente.AlterarSenha(hasher.Gerar(request.NovaSenha));
        await uow.SalvarAsync(ct);
    }

    /// <summary>Exclusão lógica: preserva o histórico de locações.</summary>
    public async Task ExcluirAsync(Guid id, CancellationToken ct)
    {
        var cliente = await CarregarAsync(id, ct);
        if (await locacoes.ClientePossuiLocacoesAtivasAsync(id, ct))
            throw new ConflitoException("O cliente possui locações reservadas ou em andamento.");
        cliente.Desativar();
        await uow.SalvarAsync(ct);
    }

    public async Task ReativarAsync(Guid id, CancellationToken ct)
    {
        var cliente = await CarregarAsync(id, ct);
        cliente.Reativar();
        await uow.SalvarAsync(ct);
    }

    internal async Task GarantirUnicidadeAsync(Cpf cpf, Email email, Guid? ignorarId, CancellationToken ct)
    {
        if (await usuarios.ExisteCpfAsync(cpf, ignorarId, ct))
            throw new ConflitoException("Já existe um usuário com este CPF.");
        if (await usuarios.ExisteEmailAsync(email, ignorarId, ct))
            throw new ConflitoException("Já existe um usuário com este e-mail.");
    }

    private async Task<Cliente> CarregarAsync(Guid id, CancellationToken ct) =>
        await usuarios.ObterClienteAsync(id, ct) ?? throw new RecursoNaoEncontradoException("Cliente");
}
