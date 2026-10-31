using LocadoraCarros.Application.Auth;
using LocadoraCarros.Application.Carros;
using LocadoraCarros.Application.Clientes;
using LocadoraCarros.Application.Contas;
using LocadoraCarros.Application.Funcionarios;
using LocadoraCarros.Application.Locacoes;
using LocadoraCarros.Application.Lojas;
using Microsoft.Extensions.DependencyInjection;

namespace LocadoraCarros.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ContaService>();
        services.AddScoped<ClienteService>();
        services.AddScoped<FuncionarioService>();
        services.AddScoped<CarroService>();
        services.AddScoped<LojaService>();
        services.AddScoped<LocacaoService>();
        return services;
    }
}
