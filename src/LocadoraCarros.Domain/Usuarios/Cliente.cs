using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Domain.Usuarios;

public sealed class Cliente : Usuario
{
    public const int IdadeMinimaCadastro = 18;
    public const int IdadeMinimaLocacao = 21;

    private Cliente() { }

    private Cliente(string nome, Cpf cpf, Email email, Telefone telefone, string senhaHash)
        : base(nome, cpf, email, telefone, senhaHash, PerfilUsuario.Cliente) { }

    public DateOnly DataNascimento { get; private set; }
    public Endereco Endereco { get; private set; } = null!;
    public Cnh Cnh { get; private set; } = null!;

    public static Cliente Criar(string nome, Cpf cpf, Email email, Telefone telefone, string senhaHash,
        DateOnly dataNascimento, Endereco endereco, Cnh cnh, DateOnly hoje)
    {
        var cliente = new Cliente(nome, cpf, email, telefone, senhaHash);
        cliente.DefinirDadosPessoais(dataNascimento, endereco, cnh, hoje);
        return cliente;
    }

    /// <summary>Atualiza todos os dados cadastrais, exceto o CPF.</summary>
    public void AtualizarCadastro(string nome, Email email, Telefone telefone, DateOnly dataNascimento,
        Endereco endereco, Cnh cnh, DateOnly hoje)
    {
        DefinirDadosBasicos(nome, email, telefone);
        DefinirDadosPessoais(dataNascimento, endereco, cnh, hoje);
    }

    public int IdadeEm(DateOnly data)
    {
        var idade = data.Year - DataNascimento.Year;
        if (DataNascimento > data.AddYears(-idade)) idade--;
        return idade;
    }

    public void ValidarAptidaoParaLocacao(DateOnly retirada, DateOnly devolucao)
    {
        Guard.Garantir(Ativo, "Cliente inativo não pode realizar locações.");
        Guard.Garantir(IdadeEm(retirada) >= IdadeMinimaLocacao,
            $"O condutor precisa ter pelo menos {IdadeMinimaLocacao} anos na data da retirada.");
        Guard.Garantir(Cnh.HabilitaAutomovel, "A categoria da CNH não permite conduzir automóveis.");
        Guard.Garantir(Cnh.ValidaEm(devolucao), "A CNH precisa estar válida até a data de devolução.");
    }

    private void DefinirDadosPessoais(DateOnly dataNascimento, Endereco endereco, Cnh cnh, DateOnly hoje)
    {
        DataNascimento = dataNascimento;
        Guard.Garantir(dataNascimento < hoje && IdadeEm(hoje) >= IdadeMinimaCadastro,
            $"O cliente precisa ter pelo menos {IdadeMinimaCadastro} anos.");
        Endereco = endereco;
        Cnh = cnh;
    }
}
