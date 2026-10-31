using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Domain.Carros;

public sealed class Carro : AggregateRoot
{
    private Carro() { }

    public Placa Placa { get; private set; } = null!;
    /// <summary>Placa no padrão Mercosul, usada como chave única entre os dois padrões.</summary>
    public string PlacaChave { get; private set; } = null!;
    public string Renavam { get; private set; } = null!;
    public string Chassi { get; private set; } = null!;
    public string Marca { get; private set; } = null!;
    public string Modelo { get; private set; } = null!;
    public string? Versao { get; private set; }
    public int AnoFabricacao { get; private set; }
    public int AnoModelo { get; private set; }
    public string Cor { get; private set; } = null!;
    public CategoriaCarro Categoria { get; private set; }
    public TipoCombustivel Combustivel { get; private set; }
    public TipoCambio Cambio { get; private set; }
    public int Portas { get; private set; }
    public int Lugares { get; private set; }
    public int PortaMalasLitros { get; private set; }
    public bool ArCondicionado { get; private set; }
    public bool DirecaoAssistida { get; private set; }
    public bool Multimidia { get; private set; }
    public int Quilometragem { get; private set; }
    public decimal ValorDiaria { get; private set; }
    public Guid LojaAtualId { get; private set; }
    public StatusCarro Status { get; private set; }
    public string? Observacoes { get; private set; }

    /// <summary>Pode receber reservas futuras: carros alugados ou em limpeza voltam a ficar disponíveis.</summary>
    public bool AceitaReservas => Status is StatusCarro.Disponivel or StatusCarro.Alugado or StatusCarro.EmLimpeza;

    public static Carro Criar(DadosCarro dados, Guid lojaId, int quilometragem, int anoAtual)
    {
        Guard.Garantir(lojaId != Guid.Empty, "Informe a loja do carro.");
        Guard.Garantir(quilometragem >= 0, "Quilometragem não pode ser negativa.");

        var carro = new Carro
        {
            LojaAtualId = lojaId,
            Quilometragem = quilometragem,
            Status = StatusCarro.Disponivel
        };
        carro.Atualizar(dados, anoAtual);
        return carro;
    }

    public void Atualizar(DadosCarro dados, int anoAtual)
    {
        var renavam = Guard.SomenteDigitos(dados.Renavam);
        Guard.Garantir(renavam.Length == 11, "RENAVAM inválido. Informe 11 dígitos.");

        var chassi = dados.Chassi?.Trim().ToUpperInvariant() ?? string.Empty;
        Guard.Garantir(chassi.Length == 17 && chassi.All(char.IsAsciiLetterOrDigit) && !chassi.Any(c => c is 'I' or 'O' or 'Q'),
            "Chassi inválido. Informe os 17 caracteres do VIN.");

        Guard.Garantir(dados.AnoFabricacao >= 1990 && dados.AnoFabricacao <= anoAtual + 1, "Ano de fabricação inválido.");
        Guard.Garantir(dados.AnoModelo >= dados.AnoFabricacao && dados.AnoModelo <= dados.AnoFabricacao + 1,
            "Ano do modelo deve ser igual ou um ano após o de fabricação.");
        Guard.Garantir(dados.Portas is >= 2 and <= 5, "Quantidade de portas inválida.");
        Guard.Garantir(dados.Lugares is >= 2 and <= 15, "Quantidade de lugares inválida.");
        Guard.Garantir(dados.PortaMalasLitros >= 0, "Capacidade do porta-malas inválida.");
        Guard.Garantir(dados.ValorDiaria > 0, "Valor da diária deve ser maior que zero.");
        Guard.Garantir(Enum.IsDefined(dados.Categoria) && Enum.IsDefined(dados.Combustivel) && Enum.IsDefined(dados.Cambio),
            "Categoria, combustível ou câmbio inválido.");

        Placa = dados.Placa;
        PlacaChave = dados.Placa.EquivalenteMercosul;
        Renavam = renavam;
        Chassi = chassi;
        Marca = Guard.Texto(dados.Marca, "Marca", 60);
        Modelo = Guard.Texto(dados.Modelo, "Modelo", 60);
        Versao = Guard.TextoOpcional(dados.Versao, "Versão", 80);
        AnoFabricacao = dados.AnoFabricacao;
        AnoModelo = dados.AnoModelo;
        Cor = Guard.Texto(dados.Cor, "Cor", 40);
        Categoria = dados.Categoria;
        Combustivel = dados.Combustivel;
        Cambio = dados.Cambio;
        Portas = dados.Portas;
        Lugares = dados.Lugares;
        PortaMalasLitros = dados.PortaMalasLitros;
        ArCondicionado = dados.ArCondicionado;
        DirecaoAssistida = dados.DirecaoAssistida;
        Multimidia = dados.Multimidia;
        ValorDiaria = decimal.Round(dados.ValorDiaria, 2);
        Observacoes = Guard.TextoOpcional(dados.Observacoes, "Observações");
    }

    /// <summary>Mudança manual de status pela equipe (manutenção, limpeza, transferência...).</summary>
    public void AlterarStatus(StatusCarro novoStatus, string? observacao)
    {
        Guard.Garantir(Enum.IsDefined(novoStatus), "Status inválido.");
        Guard.Garantir(Status != StatusCarro.Alugado,
            "Carro alugado só muda de status com o registro da devolução.");
        Guard.Garantir(novoStatus != StatusCarro.Alugado,
            "O status 'Alugado' é definido automaticamente na retirada da locação.");

        Status = novoStatus;
        if (observacao is not null)
            Observacoes = Guard.TextoOpcional(observacao, "Observações");
    }

    public void TransferirParaLoja(Guid lojaId)
    {
        Guard.Garantir(Status != StatusCarro.Alugado, "Não é possível transferir um carro alugado.");
        Guard.Garantir(lojaId != Guid.Empty, "Loja inválida.");
        LojaAtualId = lojaId;
    }

    public void Inativar()
    {
        Guard.Garantir(Status != StatusCarro.Alugado, "Não é possível excluir um carro alugado.");
        Status = StatusCarro.Inativo;
    }

    internal void RegistrarRetirada(int quilometragem)
    {
        Guard.Garantir(Status == StatusCarro.Disponivel, $"O carro não está disponível para retirada (status atual: {Status}).");
        AtualizarQuilometragem(quilometragem);
        Status = StatusCarro.Alugado;
    }

    internal void RegistrarDevolucao(int quilometragem, Guid lojaDevolucaoId)
    {
        Guard.Garantir(Status == StatusCarro.Alugado, "O carro não consta como alugado.");
        AtualizarQuilometragem(quilometragem);
        LojaAtualId = lojaDevolucaoId;
        Status = StatusCarro.EmLimpeza;
    }

    private void AtualizarQuilometragem(int quilometragem)
    {
        Guard.Garantir(quilometragem >= Quilometragem,
            $"A quilometragem informada ({quilometragem}) é menor que a registrada ({Quilometragem}).");
        Quilometragem = quilometragem;
    }
}

/// <summary>Dados técnicos e comerciais do veículo, editáveis pela equipe.</summary>
public sealed record DadosCarro(
    Placa Placa,
    string Renavam,
    string Chassi,
    string Marca,
    string Modelo,
    string? Versao,
    int AnoFabricacao,
    int AnoModelo,
    string Cor,
    CategoriaCarro Categoria,
    TipoCombustivel Combustivel,
    TipoCambio Cambio,
    int Portas,
    int Lugares,
    int PortaMalasLitros,
    bool ArCondicionado,
    bool DirecaoAssistida,
    bool Multimidia,
    decimal ValorDiaria,
    string? Observacoes);
