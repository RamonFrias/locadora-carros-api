using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.Lojas;

namespace LocadoraCarros.Domain.Locacoes;

/// <summary>Políticas comerciais da locadora.</summary>
public static class TabelaPrecos
{
    public static readonly TimeSpan ToleranciaAtraso = TimeSpan.FromHours(1);
    public static readonly TimeSpan AntecedenciaMinimaReembolso = TimeSpan.FromHours(24);
    public static readonly TimeSpan AntecedenciaMaximaRetirada = TimeSpan.FromHours(2);
    public const int MaximoDiarias = 90;
    public const int OitavosTanqueCheio = 8;
    public const decimal TaxaPorOitavoCombustivel = 45m;
    public const decimal TaxaDevolucaoMesmaCidade = 60m;
    public const decimal TaxaDevolucaoOutraCidade = 350m;

    public static decimal DiariaProtecao(TipoProtecao protecao) => protecao switch
    {
        TipoProtecao.Basica => 29.90m,
        TipoProtecao.Completa => 54.90m,
        TipoProtecao.Premium => 79.90m,
        _ => throw new DomainException("Proteção inválida.")
    };

    public static decimal DiariaAdicional(TipoAdicional adicional) => adicional switch
    {
        TipoAdicional.CondutorAdicional => 25m,
        TipoAdicional.CadeirinhaBebe => 20m,
        TipoAdicional.AssentoElevacao => 15m,
        TipoAdicional.Gps => 15m,
        TipoAdicional.InternetMovel => 19.90m,
        TipoAdicional.TagPedagio => 9.90m,
        _ => throw new DomainException("Adicional inválido.")
    };

    public static decimal Caucao(CategoriaCarro categoria) => categoria switch
    {
        CategoriaCarro.Economico or CategoriaCarro.Compacto => 700m,
        CategoriaCarro.Intermediario or CategoriaCarro.Sedan => 1_000m,
        CategoriaCarro.Suv or CategoriaCarro.Picape or CategoriaCarro.Minivan or CategoriaCarro.Utilitario => 1_500m,
        CategoriaCarro.Executivo or CategoriaCarro.Eletrico => 2_500m,
        CategoriaCarro.Luxo => 5_000m,
        _ => 1_000m
    };

    public static decimal TaxaDevolucao(Loja retirada, Loja devolucao)
    {
        if (retirada.Id == devolucao.Id) return 0m;
        return retirada.Endereco.MesmaCidade(devolucao.Endereco) ? TaxaDevolucaoMesmaCidade : TaxaDevolucaoOutraCidade;
    }

    /// <summary>Uma diária a cada 24h, com 1h de tolerância. Mínimo de uma diária.</summary>
    public static int CalcularDiarias(DateTime inicio, DateTime fim)
    {
        var horas = (fim - inicio - ToleranciaAtraso).TotalHours;
        return Math.Max(1, (int)Math.Ceiling(horas / 24));
    }
}

public sealed record Orcamento(
    int QuantidadeDiarias,
    decimal ValorDiaria,
    decimal ValorDiarias,
    decimal ValorProtecao,
    decimal ValorAdicionais,
    decimal TaxaDevolucaoOutraLoja,
    decimal ValorTotal,
    decimal ValorCaucao)
{
    public static Orcamento Calcular(Carro carro, Loja retirada, Loja devolucao, DateTime inicio, DateTime fim,
        TipoProtecao protecao, IEnumerable<AdicionalLocacao> adicionais)
    {
        var diarias = TabelaPrecos.CalcularDiarias(inicio, fim);
        var valorDiarias = carro.ValorDiaria * diarias;
        var valorProtecao = TabelaPrecos.DiariaProtecao(protecao) * diarias;
        var valorAdicionais = adicionais.Sum(a => a.ValorDiarioTotal) * diarias;
        var taxaDevolucao = TabelaPrecos.TaxaDevolucao(retirada, devolucao);

        return new Orcamento(
            diarias,
            carro.ValorDiaria,
            valorDiarias,
            valorProtecao,
            valorAdicionais,
            taxaDevolucao,
            valorDiarias + valorProtecao + valorAdicionais + taxaDevolucao,
            TabelaPrecos.Caucao(carro.Categoria));
    }
}
