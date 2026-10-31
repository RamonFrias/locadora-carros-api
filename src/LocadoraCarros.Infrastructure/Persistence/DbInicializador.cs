using LocadoraCarros.Application.Abstractions;
using LocadoraCarros.Domain.Carros;
using LocadoraCarros.Domain.Lojas;
using LocadoraCarros.Domain.Usuarios;
using LocadoraCarros.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LocadoraCarros.Infrastructure.Persistence;

public static class DbInicializador
{
    /// <summary>
    /// Cria o banco (se não existir) e, quando Seed:Habilitado = true, insere lojas, equipe, cliente e frota de exemplo.
    /// Para produção, troque o EnsureCreated por migrations do EF Core.
    /// </summary>
    public static async Task InicializarAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!configuration.GetValue<bool>("Seed:Habilitado") || await db.Usuarios.AnyAsync())
            return;

        var senha = configuration["Seed:SenhaPadrao"]
            ?? throw new InvalidOperationException("Configure Seed:SenhaPadrao para popular o banco.");
        var hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().Gerar(senha);
        var hoje = scope.ServiceProvider.GetRequiredService<IRelogio>().Hoje;

        var congonhas = Loja.Criar("SP-CGH", "Aeroporto de Congonhas", Cnpj.Criar("11222333000181"), TipoLoja.Aeroporto,
            Endereco.Criar("Av. Washington Luís", "s/n", "Terminal de passageiros", "Vila Congonhas", "São Paulo", "SP", "04626911"),
            Telefone.Criar("1150901000"), Email.Criar("congonhas@locadora.dev"), true, default, default, true);
        var paulista = Loja.Criar("SP-PAU", "Avenida Paulista", Cnpj.Criar("11222333000262"), TipoLoja.Rua,
            Endereco.Criar("Av. Paulista", "1000", null, "Bela Vista", "São Paulo", "SP", "01310100"),
            Telefone.Criar("1132851000"), Email.Criar("paulista@locadora.dev"), false, new TimeOnly(7, 0), new TimeOnly(22, 0), true);
        var rio = Loja.Criar("RJ-CEN", "Rio de Janeiro Centro", Cnpj.Criar("11222333000343"), TipoLoja.Rua,
            Endereco.Criar("Av. Rio Branco", "156", "Loja A", "Centro", "Rio de Janeiro", "RJ", "20040003"),
            Telefone.Criar("2125321000"), Email.Criar("riocentro@locadora.dev"), false, new TimeOnly(8, 0), new TimeOnly(20, 0), true);
        db.Lojas.AddRange(congonhas, paulista, rio);

        db.Usuarios.AddRange(
            Gerente.Criar("Mariana Souza", Cpf.Criar("12345678909"), Email.Criar("gerente@locadora.dev"),
                Telefone.Criar("11987650001"), hash, "G0001", "Gerente Regional", paulista.Id, new DateOnly(2020, 3, 2)),
            Funcionario.Criar("Carlos Lima", Cpf.Criar("98765432100"), Email.Criar("funcionario@locadora.dev"),
                Telefone.Criar("11987650002"), hash, "F0001", "Atendente de Balcão", congonhas.Id, new DateOnly(2023, 8, 14)),
            Cliente.Criar("João Pereira", Cpf.Criar("52998224725"), Email.Criar("cliente@locadora.dev"),
                Telefone.Criar("11987650003"), hash, new DateOnly(1990, 5, 10),
                Endereco.Criar("Rua Augusta", "500", "Apto 12", "Consolação", "São Paulo", "SP", "01305000"),
                Cnh.Criar("12345678901", "B", new DateOnly(2031, 5, 10)), hoje));

        var ano = hoje.Year;
        Carro NovoCarro(string placa, string renavam, string chassi, string marca, string modelo, string versao,
            int fabricacao, string cor, CategoriaCarro categoria, TipoCombustivel combustivel, TipoCambio cambio,
            int portas, int lugares, int portaMalas, decimal diaria, Loja loja, int km) =>
            Carro.Criar(new DadosCarro(Placa.Criar(placa), renavam, chassi, marca, modelo, versao, fabricacao, fabricacao + 1,
                cor, categoria, combustivel, cambio, portas, lugares, portaMalas, true, true, true, diaria, null), loja.Id, km, ano);

        db.Carros.AddRange(
            NovoCarro("FBR2E19", "00123456781", "9BD341A5XRY123456", "Fiat", "Mobi", "Like 1.0", 2024, "Branco",
                CategoriaCarro.Economico, TipoCombustivel.Flex, TipoCambio.Manual, 4, 5, 215, 119.90m, congonhas, 18_500),
            NovoCarro("GHT4521", "00123456782", "9BGKS48U0NG100001", "Chevrolet", "Onix", "LT 1.0", 2022, "Prata",
                CategoriaCarro.Compacto, TipoCombustivel.Flex, TipoCambio.Manual, 4, 5, 303, 139.90m, congonhas, 42_300),
            NovoCarro("RTA7C88", "00123456783", "9BWDH6BF5RP000123", "Volkswagen", "Virtus", "Highline 1.4 TSI", 2024, "Cinza",
                CategoriaCarro.Sedan, TipoCombustivel.Flex, TipoCambio.Automatico, 4, 5, 521, 219.90m, congonhas, 9_800),
            NovoCarro("QXZ1A23", "00123456784", "988675113PK000777", "Jeep", "Compass", "Longitude T270", 2023, "Preto",
                CategoriaCarro.Suv, TipoCombustivel.Flex, TipoCambio.Automatico, 4, 5, 410, 329.90m, paulista, 27_100),
            NovoCarro("BYD0E21", "00123456785", "LGXCE4CB5S0001234", "BYD", "Dolphin", "GS", 2025, "Azul",
                CategoriaCarro.Eletrico, TipoCombustivel.Eletrico, TipoCambio.Automatico, 4, 5, 345, 289.90m, paulista, 3_200),
            NovoCarro("KPL3H45", "00123456786", "9BRKZAAG5S0123456", "Toyota", "Corolla Cross", "XRE Hybrid", 2024, "Branco",
                CategoriaCarro.Suv, TipoCombustivel.Hibrido, TipoCambio.Cvt, 4, 5, 440, 299.90m, rio, 15_700),
            NovoCarro("LMN8842", "00123456787", "9BD281A2DPY654321", "Fiat", "Strada", "Freedom Cabine Simples", 2023, "Vermelho",
                CategoriaCarro.Picape, TipoCombustivel.Flex, TipoCambio.Manual, 2, 2, 844, 189.90m, rio, 33_400),
            NovoCarro("RIO2B34", "00123456788", "9BHBG51CAPP112233", "Hyundai", "HB20", "Comfort 1.0", 2023, "Prata",
                CategoriaCarro.Compacto, TipoCombustivel.Flex, TipoCambio.Manual, 4, 5, 300, 129.90m, rio, 21_900));

        await db.SaveChangesAsync();
    }
}
