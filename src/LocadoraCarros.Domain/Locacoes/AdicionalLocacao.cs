using LocadoraCarros.Domain.Common;

namespace LocadoraCarros.Domain.Locacoes;

/// <summary>Item adicional contratado. O valor da diária fica congelado no momento da reserva.</summary>
public sealed class AdicionalLocacao
{
    private AdicionalLocacao() { }

    public TipoAdicional Tipo { get; private set; }
    public int Quantidade { get; private set; }
    public decimal ValorDiario { get; private set; }

    public decimal ValorDiarioTotal => ValorDiario * Quantidade;

    public static AdicionalLocacao Criar(TipoAdicional tipo, int quantidade)
    {
        Guard.Garantir(Enum.IsDefined(tipo), "Adicional inválido.");
        Guard.Garantir(quantidade is >= 1 and <= 4, "Quantidade do adicional deve estar entre 1 e 4.");
        return new AdicionalLocacao
        {
            Tipo = tipo,
            Quantidade = quantidade,
            ValorDiario = TabelaPrecos.DiariaAdicional(tipo)
        };
    }
}
