using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Locacoes;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Domain.Tests;

internal static class Fabrica
{
    public static readonly DateTime Agora = new(2026, 11, 2, 10, 0, 0);
    public static DateOnly Hoje => DateOnly.FromDateTime(Agora);

    public static Loja Loja(string codigo = "SP-01", string cidade = "São Paulo", string uf = "SP",
        string cnpj = "11222333000181") =>
        Lojas.Loja.Criar(codigo, $"Loja {codigo}", Cnpj.Criar(cnpj), TipoLoja.Rua,
            Endereco.Criar("Rua A", "1", null, "Centro", cidade, uf, "01001000"),
            Telefone.Criar("1130000000"), Email.Criar($"{codigo.ToLowerInvariant()}@locadora.dev"),
            true, default, default, true);

    public static Cliente Cliente(string cpf = "52998224725", DateOnly? nascimento = null) =>
        Usuarios.Cliente.Criar("Cliente Teste", Cpf.Criar(cpf), Email.Criar($"{cpf}@teste.dev"),
            Telefone.Criar("11999998888"), "hash", nascimento ?? new DateOnly(1990, 1, 1),
            Endereco.Criar("Rua B", "2", null, "Centro", "São Paulo", "SP", "01001000"),
            Cnh.Criar("12345678901", "B", new DateOnly(2030, 1, 1)), Hoje);

    public static Funcionario Funcionario(Guid lojaId) =>
        Usuarios.Funcionario.Criar("Atendente", Cpf.Criar("98765432100"), Email.Criar("atendente@teste.dev"),
            Telefone.Criar("11988887777"), "hash", "F1", "Atendente", lojaId, new DateOnly(2024, 1, 1));

    public static Carro Carro(Loja loja, decimal diaria = 100m) =>
        Carros.Carro.Criar(new DadosCarro(Placa.Criar("ABC1D23"), "12345678901", "9BD341A5XRY123456", "Fiat", "Mobi",
            null, 2024, 2025, "Branco", CategoriaCarro.Economico, TipoCombustivel.Flex, TipoCambio.Manual, 4, 5, 200,
            true, true, false, diaria, null), loja.Id, 1_000, 2026);

    public static Locacao Reservar(Cliente cliente, Carro carro, Loja retirada, Loja devolucao, DateTime inicio,
        DateTime fim, Usuario reservadaPor, FormaPagamento forma = FormaPagamento.Pix,
        params AdicionalLocacao[] adicionais) =>
        Locacao.Criar(new DadosReserva(cliente, carro, retirada, devolucao, inicio, fim, TipoProtecao.Basica,
            adicionais, forma, null), reservadaPor, Agora);
}
