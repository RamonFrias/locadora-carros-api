namespace LocadoraCarros.Domain.Locacoes;

public enum StatusLocacao
{
    Reservada = 1,
    EmAndamento = 2,
    Concluida = 3,
    Cancelada = 4
}

public enum FormaPagamento
{
    CartaoCredito = 1,
    CartaoDebito = 2,
    Pix = 3,
    Dinheiro = 4 // pago no balcão, na retirada
}

public enum StatusPagamento
{
    Pendente = 1,
    ParcialmentePago = 2,
    Pago = 3,
    Reembolsado = 4,
    NaoReembolsado = 5, // cancelamento fora do prazo: valor retido
    Cancelado = 6       // cancelada sem nenhum pagamento
}

public enum StatusCaucao
{
    Pendente = 1,          // será bloqueada na retirada
    Bloqueada = 2,
    Liberada = 3,
    ParcialmenteRetida = 4,
    Retida = 5,
    NaoAplicavel = 6       // locação cancelada antes da retirada
}

public enum TipoProtecao
{
    Basica = 1,   // terceiros
    Completa = 2, // terceiros + colisão/roubo com coparticipação
    Premium = 3   // cobertura total, sem coparticipação
}

public enum TipoAdicional
{
    CondutorAdicional = 1,
    CadeirinhaBebe = 2,
    AssentoElevacao = 3,
    Gps = 4,
    InternetMovel = 5,
    TagPedagio = 6
}
