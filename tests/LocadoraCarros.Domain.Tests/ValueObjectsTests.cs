using LocadoraCarros.Domain.Common;
using LocadoraCarros.Domain.ValueObjects;

namespace LocadoraCarros.Domain.Tests;

public class ValueObjectsTests
{
    [Fact]
    public void Cpf_valido_e_normalizado_e_formatado()
    {
        var cpf = Cpf.Criar("529.982.247-25");
        Assert.Equal("52998224725", cpf.Valor);
        Assert.Equal("529.982.247-25", cpf.Formatado);
    }

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("529.982.247-24")]
    [InlineData("123")]
    [InlineData("")]
    public void Cpf_invalido_e_rejeitado(string valor) =>
        Assert.Throws<DomainException>(() => Cpf.Criar(valor));

    [Theory]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("12.ABC.345/01DE-35", "12ABC34501DE35")] // CNPJ alfanumérico (Receita Federal, 2026)
    public void Cnpj_numerico_e_alfanumerico_sao_aceitos(string entrada, string esperado) =>
        Assert.Equal(esperado, Cnpj.Criar(entrada).Valor);

    [Fact]
    public void Cnpj_com_digito_errado_e_rejeitado() =>
        Assert.Throws<DomainException>(() => Cnpj.Criar("11.222.333/0001-82"));

    [Theory]
    [InlineData("abc-1234", PadraoPlaca.Antiga, "ABC-1234", "ABC1C34")]
    [InlineData("ABC1D23", PadraoPlaca.Mercosul, "ABC1D23", "ABC1D23")]
    [InlineData("xyz 9876", PadraoPlaca.Antiga, "XYZ-9876", "XYZ9I76")]
    public void Placa_reconhece_padrao_e_equivalente_mercosul(string entrada, PadraoPlaca padrao, string formatada, string mercosul)
    {
        var placa = Placa.Criar(entrada);
        Assert.Equal(padrao, placa.Padrao);
        Assert.Equal(formatada, placa.Formatada);
        Assert.Equal(mercosul, placa.EquivalenteMercosul);
    }

    [Theory]
    [InlineData("AB12345")]
    [InlineData("ABCD123")]
    [InlineData("ABC12D3")]
    public void Placa_invalida_e_rejeitada(string valor) =>
        Assert.Throws<DomainException>(() => Placa.Criar(valor));

    [Fact]
    public void Telefone_aceita_ddi_e_formata()
    {
        Assert.Equal("(11) 98765-4321", Telefone.Criar("+55 11 98765-4321").Formatado);
        Assert.Equal("(21) 3232-1000", Telefone.Criar("2132321000").Formatado);
    }
}
