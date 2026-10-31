using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Domain.Lojas;

public enum TipoLoja
{
    Rua = 1,
    Aeroporto = 2,
    Rodoviaria = 3,
    Shopping = 4
}

public sealed class Loja : AggregateRoot
{
    private Loja() { }

    public string Codigo { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public Cnpj Cnpj { get; private set; } = null!;
    public TipoLoja Tipo { get; private set; }
    public Endereco Endereco { get; private set; } = null!;
    public Telefone Telefone { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public bool Funciona24Horas { get; private set; }
    public TimeOnly HorarioAbertura { get; private set; }
    public TimeOnly HorarioFechamento { get; private set; }
    public bool AceitaDevolucaoDeOutrasLojas { get; private set; }
    public bool Ativa { get; private set; } = true;

    public static Loja Criar(string codigo, string nome, Cnpj cnpj, TipoLoja tipo, Endereco endereco,
        Telefone telefone, Email email, bool funciona24Horas, TimeOnly horarioAbertura, TimeOnly horarioFechamento,
        bool aceitaDevolucaoDeOutrasLojas)
    {
        var loja = new Loja { Codigo = Guard.Texto(codigo, "Código da loja", 20).ToUpperInvariant() };
        loja.Atualizar(nome, cnpj, tipo, endereco, telefone, email, funciona24Horas, horarioAbertura,
            horarioFechamento, aceitaDevolucaoDeOutrasLojas);
        return loja;
    }

    public void Atualizar(string nome, Cnpj cnpj, TipoLoja tipo, Endereco endereco, Telefone telefone,
        Email email, bool funciona24Horas, TimeOnly horarioAbertura, TimeOnly horarioFechamento,
        bool aceitaDevolucaoDeOutrasLojas)
    {
        Guard.Garantir(Enum.IsDefined(tipo), "Tipo de loja inválido.");
        Guard.Garantir(funciona24Horas || horarioAbertura != horarioFechamento,
            "Horário de abertura e fechamento não podem ser iguais.");

        Nome = Guard.Texto(nome, "Nome da loja");
        Cnpj = cnpj;
        Tipo = tipo;
        Endereco = endereco;
        Telefone = telefone;
        Email = email;
        Funciona24Horas = funciona24Horas;
        HorarioAbertura = funciona24Horas ? TimeOnly.MinValue : horarioAbertura;
        HorarioFechamento = funciona24Horas ? new TimeOnly(23, 59, 59) : horarioFechamento; // sem fração: bancos arredondam MaxValue para 24:00
        AceitaDevolucaoDeOutrasLojas = aceitaDevolucaoDeOutrasLojas;
    }

    public void Desativar() => Ativa = false;

    public void Reativar() => Ativa = true;

    public bool EstaAbertaEm(DateTime dataHora)
    {
        if (Funciona24Horas) return true;
        var hora = TimeOnly.FromDateTime(dataHora);
        return HorarioAbertura < HorarioFechamento
            ? hora >= HorarioAbertura && hora <= HorarioFechamento
            : hora >= HorarioAbertura || hora <= HorarioFechamento; // expediente que atravessa a meia-noite
    }
}
