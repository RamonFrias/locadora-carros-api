namespace LocadoraCarros.Domain.Carros;

public enum StatusCarro
{
    Disponivel = 1,
    Alugado = 2,
    EmManutencao = 3,
    EmLimpeza = 4,
    EmTransferencia = 5,
    Indisponivel = 6, // sinistro, documentação pendente, recall...
    Inativo = 7       // excluído da frota (vendido/baixado)
}

public enum CategoriaCarro
{
    Economico = 1,
    Compacto = 2,
    Intermediario = 3,
    Sedan = 4,
    Suv = 5,
    Picape = 6,
    Minivan = 7,
    Executivo = 8,
    Luxo = 9,
    Eletrico = 10,
    Utilitario = 11
}

public enum TipoCombustivel
{
    Flex = 1,
    Gasolina = 2,
    Etanol = 3,
    Diesel = 4,
    Gnv = 5,
    Hibrido = 6,
    Eletrico = 7
}

public enum TipoCambio
{
    Manual = 1,
    Automatico = 2,
    Cvt = 3,
    Automatizado = 4
}
